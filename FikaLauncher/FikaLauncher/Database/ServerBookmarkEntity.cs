using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FikaLauncher.ViewModels;

namespace FikaLauncher.Database;

[Table("ServerBookmarks")]
public class ServerBookmarkEntity : INotifyPropertyChanged
{
    [Key] public int Id { get; set; }

    [Required] public int UserId { get; set; }

    [Required] [MaxLength(255)] public string ServerAddress { get; set; } = string.Empty;

    [Required] [Range(1, 65535)] public int ServerPort { get; set; }

    private string? _bookmarkName;

    [MaxLength(50)]
    public string? BookmarkName
    {
        get => _bookmarkName;
        set
        {
            if (_bookmarkName != value)
            {
                _bookmarkName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string DisplayName => !string.IsNullOrWhiteSpace(BookmarkName)
        ? BookmarkName
        : FullServerAddress;

    [NotMapped] public string FullServerAddress => $"{ServerAddress}:{ServerPort}";

    [ForeignKey("UserId")] public UserEntity User { get; set; } = null!;

    private bool _isEditing;

    [NotMapped]
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (_isEditing != value)
            {
                _isEditing = value;
                if (value) ShouldFocus = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShouldFocus));
            }
        }
    }

    [NotMapped] public bool ShouldFocus { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static ServerBookmarkEntity FromFullAddress(string fullAddress, int userId, string? bookmarkName = null)
    {
        var parts = fullAddress.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            throw new ArgumentException("Invalid server address format. Expected format: address:port");

        return new ServerBookmarkEntity
        {
            UserId = userId,
            ServerAddress = parts[0],
            ServerPort = port,
            BookmarkName = bookmarkName
        };
    }

    public void UpdateBookmarkName(string? newName)
    {
        BookmarkName = !string.IsNullOrWhiteSpace(newName)
            ? newName.Trim()
            : null;
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ServerAddress) &&
               ServerPort is >= 1 and <= 65535 &&
               (BookmarkName == null || BookmarkName.Length <= 50);
    }

    public void CancelEditing()
    {
        IsEditing = false;
    }
}