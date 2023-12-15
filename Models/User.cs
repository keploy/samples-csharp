// User.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserDataAPI;

namespace UserDataAPI
{
    [Table("Users", Schema = "public")] // Adjust the schema if needed
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string? Name { get; set; }

        public int Age { get; set; }
    }
}
