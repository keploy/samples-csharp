#!/usr/bin/env bash
# measure.sh: record the suite once with every dependency up, stop every dependency, then replay it N times.
# Run from crm-playwright/ after the README's setup (the app published to app/out, e2e/'s packages and browser
# installed, the providers' CA trusted, keploy installed).
#   measure.sh [N]         record, then N replays (default 10)
#   measure.sh record      deps up, KEPLOY_PROVEN=1 keploy mock record --local, deps down, then no container left
#                          and each port the app calls checked closed
#   measure.sh replay [N]  each port the app calls checked closed and no dependency container left (when the
#                          docker command exists), then N replays of the recording in keploy/
#                          (KEPLOY_PROVEN=1 keploy mock replay --local)
#   measure.sh check LOG.. the replay checks on saved replay logs
# Exit 0 only when record and every replay exit 0 and each replay's own count line
# ("N test(s) ran: P passed, F failed ...; Q quarantined") counts no failed test, at least MIN_COMPARED passed
# tests (default 1), and the same counts as the first replay (and as EXPECT_COUNTS, "ran/passed/failed/quarantined",
# when set). Logs go to OUT (default measure-out/). The last line says MEASURE: PASS or MEASURE: FAIL.
# RECORD_FLAGS and REPLAY_FLAGS add keploy flags (e.g. --debug). Each keploy run that has not ended after
# KEPLOY_TIMEOUT seconds (default 1500) is stopped and fails the measure.
set -uo pipefail
usage() {
  echo "usage: measure.sh [N] | record | replay [N] | check REPLAY_LOG..." >&2
  exit 2
}
# A whole number from 1: zero replays would measure nothing and pass.
count() { case "$1" in '' | *[!0-9]* | 0*) usage ;; esac; }
mode=${1:-10}
[ "$#" -gt 0 ] && shift
logs=()
case "$mode" in
record) ;;
replay) n=${1:-10}; count "$n" ;;
check)
  [ "$#" -gt 0 ] || usage
  # The logs as given, from the caller's directory.
  for f in "$@"; do logs+=("$(realpath -- "$f")") || exit 2; done
  ;;
*) n=$mode; count "$n"; mode=all ;;
esac
cd "$(dirname "$0")/.." || exit 2
OUT=${OUT:-measure-out}
MIN_COMPARED=${MIN_COMPARED:-1}
EXPECT_COUNTS=${EXPECT_COUNTS:-}
FAILS=""
FIRST=""
mkdir -p "$OUT"
log() { echo "measure: $*"; }
fail() {
  log "FAIL: $*"
  FAILS="$FAILS
  - $*"
}
export KEPLOY_PROVEN=1

# keploy_run record|replay FLAGS: keploy mock with --local and the flags, words split from FLAGS.
keploy_run() {
  local flags
  read -ra flags <<<"$2"
  timeout -s INT -k 60 "${KEPLOY_TIMEOUT:-1500}" keploy mock "$1" --local "${flags[@]}" </dev/null
}

record() {
  # Stopped half way, the dependencies still go down.
  trap 'sh scripts/deps.sh down >/dev/null 2>&1; exit 130' INT TERM
  if ! sh scripts/deps.sh up >"$OUT/deps-up.log" 2>&1; then
    tail -20 "$OUT/deps-up.log"
    fail "deps up ($OUT/deps-up.log)"
    sh scripts/deps.sh down >/dev/null 2>&1
    trap - INT TERM
    return
  fi
  log "deps up"
  local rc=0
  keploy_run record "${RECORD_FLAGS:-}" >"$OUT/record.log" 2>&1 || rc=$?
  log "record exit $rc ($OUT/record.log)"
  [ "$rc" = 0 ] || fail "record exit $rc"
  sh scripts/deps.sh down >"$OUT/deps-down.log" 2>&1 || fail "deps down ($OUT/deps-down.log)"
  trap - INT TERM
  deps_gone "after deps down"
}

# The ports the app calls (app/appsettings.json): DynamoDB 8000, SQS 9324, Pulsar 6650, the payments provider 8443,
# the LMS 9443.
PORTS="8000 9324 6650 8443 9443"

# deps_gone WHEN: every dependency gone: none of the sample's containers, and nothing answers on a port the app
# calls. On a machine with no docker command (recorded elsewhere, replayed here) only the ports are checked, and a
# line says so.
deps_gone() {
  local ids p before=$FAILS containers="no container"
  if ! command -v docker >/dev/null; then
    containers="no docker command, containers not checked"
    log "$containers $1"
  elif ! ids=$(docker compose ps -aq); then
    fail "docker compose ps failed $1"
  elif [ -n "$ids" ]; then
    fail "containers still there $1: $(docker compose ps -a --format '{{.Name}}' | tr '\n' ' ')"
  fi
  for p in $PORTS; do
    ! (exec 3<>"/dev/tcp/127.0.0.1/$p") 2>/dev/null || fail "port $p still answers $1"
  done
  [ "$FAILS" != "$before" ] || log "no dependency left $1: $containers; $PORTS closed"
}

# check NAME LOG: keploy's own count line of one replay, e.g. "keploy: crm-playwright: 1 test(s) ran: 1 passed,
# 0 failed (0 held by keploy), 0 skipped by the test; 6 quarantined (skipped by keploy)".
check() {
  local name=$1 file=$2 line c n p f
  line=$(sed 's/\x1b\[[0-9;]*m//g' "$file" | grep -E '^keploy: .*: [0-9]+ test\(s\) ran: ')
  c=$(printf '%s\n' "$line" | sed -nE 's/.* ([0-9]+) test\(s\) ran: ([0-9]+) passed, ([0-9]+) failed .*; ([0-9]+) quarantined.*/\1\/\2\/\3\/\4/p')
  if [ "$(printf '%s\n' "$line" | grep -c .)" != 1 ] || [ -z "$c" ]; then
    fail "$name: no single 'test(s) ran:' count line"
    return
  fi
  IFS=/ read -r n p f _ <<<"$c"
  log "$name: ran/passed/failed/quarantined = $c"
  [ "$f" = 0 ] || fail "$name: $f compared test(s) failed"
  [ "$p" -ge "$MIN_COMPARED" ] || fail "$name: $p of $n test(s) passed, fewer than MIN_COMPARED=$MIN_COMPARED"
  [ -z "$EXPECT_COUNTS" ] || [ "$c" = "$EXPECT_COUNTS" ] || fail "$name: counts $c, expected $EXPECT_COUNTS"
  if [ -z "$FIRST" ]; then
    FIRST=$c
  elif [ "$c" != "$FIRST" ]; then
    fail "$name: counts $c, the first replay counted $FIRST: the same recording replayed differently"
  fi
}

replay() {
  local i rc
  for i in $(seq 1 "$1"); do
    rc=0
    keploy_run replay "${REPLAY_FLAGS:-}" >"$OUT/replay-$i.log" 2>&1 || rc=$?
    log "replay $i exit $rc ($OUT/replay-$i.log)"
    [ "$rc" = 0 ] || fail "replay $i exit $rc"
    check "replay $i" "$OUT/replay-$i.log"
  done
}

verdict() {
  if [ -n "$FAILS" ]; then
    log "MEASURE: FAIL ($1):$FAILS"
    exit 1
  fi
  log "MEASURE: PASS ($1${FIRST:+; each replay counted ran/passed/failed/quarantined = $FIRST})"
  exit 0
}

case "$mode" in
record)
  record
  verdict "record"
  ;;
replay)
  # A replay with a dependency up proves nothing about replaying without it.
  deps_gone "before the replays"
  [ -n "$FAILS" ] || replay "$n"
  verdict "$n replays"
  ;;
check)
  for f in "${logs[@]}"; do check "$f" "$f"; done
  verdict "${#logs[@]} replay logs"
  ;;
all)
  record
  # A failed record leaves nothing to measure.
  [ -n "$FAILS" ] || replay "$n"
  verdict "record and $n replays"
  ;;
esac
