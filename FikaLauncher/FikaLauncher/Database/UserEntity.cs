using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FikaLauncher.Database;

[Table("Users")]
public class UserEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string EncryptedPassword { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime LastLoginAt { get; set; }
    
    public bool IsActive { get; set; }
    
    public string? SecurityToken { get; set; }
    
    public DateTime? TokenExpiration { get; set; }

    // Add navigation property for server bookmarks
    public virtual ICollection<ServerBookmarkEntity> ServerBookmarks { get; set; } = new List<ServerBookmarkEntity>();
}
