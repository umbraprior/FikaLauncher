using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FikaLauncher.Database;
using Jeek.Avalonia.Localization;

namespace FikaLauncher.Services;

public static class UserService
{
    public static (bool isValid, string error) ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Username cannot be empty");
            
        if (username.Length < 3)
            return (false, "Username must be at least 3 characters long");
            
        if (username.Length > 20)
            return (false, "Username must be less than 20 characters");
            
        if (!username.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            return (false, "Username can only contain letters, numbers, underscores, and hyphens");
            
        return (true, string.Empty);
    }

    public static (bool isValid, string error) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password cannot be empty");
            
        if (password.Length < 8)
            return (false, "Password must be at least 8 characters long");
            
        if (!password.Any(char.IsUpper))
            return (false, "Password must contain at least one uppercase letter");
            
        if (!password.Any(char.IsLower))
            return (false, "Password must contain at least one lowercase letter");
            
        if (!password.Any(char.IsDigit))
            return (false, "Password must contain at least one number");
            
        return (true, string.Empty);
    }

    public static bool UserExists(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        username = username.ToLower();
        
        try
        {
            using var context = new AppDbContext();
            return context.Users.Any(u => u.Username == username);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking if user exists: {ex.Message}");
            return false;
        }
    }

    public static async Task<(bool success, string? token, string error)> CreateUser(string username, string password)
    {
        var (isUsernameValid, usernameError) = ValidateUsername(username);
        if (!isUsernameValid)
            return (false, null, usernameError);

        var (isPasswordValid, passwordError) = ValidatePassword(password);
        if (!isPasswordValid)
            return (false, null, passwordError);

        username = username.ToLower();
        
        try
        {
            using var context = new AppDbContext();
            
            if (await context.Users.AnyAsync(u => u.Username == username))
                return (false, null, "Username already exists");

            var encryptedPassword = DatabaseService.EncryptPassword(password);
            
            var user = new UserEntity
            {
                Username = username,
                EncryptedPassword = encryptedPassword,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                IsActive = true
            };
            
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Generate token after user is created
            var (tokenSuccess, token) = await DatabaseService.GenerateSecurityToken(username);
            if (!tokenSuccess || token == null)
            {
                return (false, null, "Failed to generate security token");
            }
            
            return (true, token, string.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating user: {ex.Message}");
            return (false, null, "Database error occurred");
        }
    }

    public static async Task<bool> ValidateUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        username = username.ToLower();
        
        try
        {
            using var context = new AppDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (user == null || !user.IsActive)
                return false;

            var decryptedPassword = DatabaseService.DecryptPassword(user.EncryptedPassword);
            if (password != decryptedPassword)
                return false;

            // Sync the database and application state
            await DatabaseService.SyncUserState(username);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating user: {ex.Message}");
            return false;
        }
    }
}
