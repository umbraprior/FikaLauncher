using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FikaLauncher.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace FikaLauncher.Services;

public static class DatabaseService
{
    private static readonly string EncryptionKey = "PR0J3CTF1K4RUL3S1337!";
    private static readonly string TokenKey = "T0K3NK3Y!2024FIKA";
    
    public static void Initialize()
    {
        try
        {
            using var context = new AppDbContext();

            context.Database.EnsureCreated();
            
            CreateDatabaseViews(context);
            
            Console.WriteLine("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing database: {ex.Message}");
            throw;
        }
    }
    
    private static void CreateDatabaseViews(AppDbContext context)
    {
        var dropViewsSql = @"
            DROP VIEW IF EXISTS UsersView;
            DROP VIEW IF EXISTS ServerBookmarksView;";
        
        context.Database.ExecuteSqlRaw(dropViewsSql);

        var usersViewSql = @"
            CREATE VIEW UsersView AS
            SELECT 
                Id,
                Username,
                CreatedAt,
                LastLoginAt,
                IsActive
            FROM Users
            ORDER BY CreatedAt DESC;";
        
        var bookmarksViewSql = @"
            CREATE VIEW ServerBookmarksView AS
            SELECT 
                sb.UserId,
                u.Username,
                GROUP_CONCAT(
                    COALESCE(sb.BookmarkName, sb.ServerAddress || ':' || sb.ServerPort)
                    || ' (' || sb.ServerAddress || ':' || sb.ServerPort || ')'
                    || ' [' || datetime(sb.CreatedAt) || ']'
                    , CHAR(10)) as UserBookmarks
            FROM ServerBookmarks sb
            JOIN Users u ON u.Id = sb.UserId
            GROUP BY sb.UserId, u.Username
            ORDER BY u.Username;";
        
        context.Database.ExecuteSqlRaw(usersViewSql);
        context.Database.ExecuteSqlRaw(bookmarksViewSql);
    }
    
    public static string EncryptPassword(string password)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32));
        aes.IV = new byte[16];
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(password);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }
    
    public static string DecryptPassword(string encryptedPassword)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32));
        aes.IV = new byte[16];
        
        using var decryptor = aes.CreateDecryptor();
        var encryptedBytes = Convert.FromBase64String(encryptedPassword);
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
    
    public static async Task SyncUserState(string username)
    {
        try
        {
            using var context = new AppDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                
                if (string.IsNullOrEmpty(user.SecurityToken))
                {
                    user.SecurityToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                    user.TokenExpiration = DateTime.UtcNow.AddDays(30);
                }
                
                await context.SaveChangesAsync();
                
                ApplicationStateService.SaveLoginState(username, true, user.SecurityToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error syncing user state: {ex.Message}");
        }
    }

    public static async Task<(bool success, string? token)> GenerateSecurityToken(string username)
    {
        try
        {
            using var context = new AppDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (user == null)
            {
                Console.WriteLine($"Failed to generate token: User {username} not found");
                return (false, null);
            }

            var rawToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            user.SecurityToken = EncryptToken(rawToken);
            user.TokenExpiration = DateTime.UtcNow.AddDays(30);
            user.LastLoginAt = DateTime.UtcNow;
            
            await context.SaveChangesAsync();
            return (true, rawToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating security token: {ex.Message}");
            return (false, null);
        }
    }

    public static async Task<(bool success, string? token)> ValidateOrRefreshSecurityToken(string username, string? existingToken)
    {
        try
        {
            using var context = new AppDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => 
                u.Username == username && 
                u.IsActive);

            if (user == null)
                return (false, null);

            if (user.TokenExpiration <= DateTime.UtcNow || string.IsNullOrEmpty(user.SecurityToken))
            {
                var rawToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                user.SecurityToken = EncryptToken(rawToken);
                user.TokenExpiration = DateTime.UtcNow.AddDays(30);
                await context.SaveChangesAsync();
                return (true, rawToken);
            }

            if (existingToken != null)
            {
                var encryptedToken = EncryptToken(existingToken);
                if (user.SecurityToken == encryptedToken)
                {
                    return (true, existingToken);
                }
            }

            var newToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            user.SecurityToken = EncryptToken(newToken);
            user.TokenExpiration = DateTime.UtcNow.AddDays(30);
            await context.SaveChangesAsync();
            return (true, newToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating/refreshing security token: {ex.Message}");
            return (false, null);
        }
    }

    public static async Task InvalidateSecurityToken(string username)
    {
        try
        {
            using var context = new AppDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (user != null)
            {
                user.SecurityToken = null;
                user.TokenExpiration = null;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error invalidating security token: {ex.Message}");
        }
    }

    private static string EncryptToken(string token)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(TokenKey.PadRight(32));
        aes.IV = new byte[16];
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(token);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }

    private static string DecryptToken(string encryptedToken)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(TokenKey.PadRight(32));
        aes.IV = new byte[16];
        
        using var decryptor = aes.CreateDecryptor();
        var encryptedBytes = Convert.FromBase64String(encryptedToken);
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    public static async Task<bool> UpdateBookmarkName(int bookmarkId, string? newName, string username)
    {
        try
        {
            using var context = new AppDbContext();
            var bookmark = await context.ServerBookmarks
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookmarkId && b.User.Username == username);

            if (bookmark == null)
                return false;

            bookmark.UpdateBookmarkName(newName);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating bookmark name: {ex.Message}");
            return false;
        }
    }

    public static async Task<List<ServerBookmarkEntity>> GetUserBookmarks(string username)
    {
        try
        {
            using var context = new AppDbContext();
            return await context.ServerBookmarks
                .Include(b => b.User)
                .Where(b => b.User.Username == username)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user bookmarks: {ex.Message}");
            return new List<ServerBookmarkEntity>();
        }
    }
}
