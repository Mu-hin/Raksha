using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Raksha.Application.DTOs.Identity;
using Raksha.Application.Interfaces.Services;
using Raksha.Application.Models;
using Raksha.Domain.Common;
using Raksha.Domain.Entities;
using Raksha.Infrastructure.Data;

namespace Raksha.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _dbContext;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
        }

        #region User Lookup

        public async Task<IdentityUserDto?> FindByIdAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return MapToDto(user, roles);
        }

        public async Task<IdentityUserDto?> FindByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return MapToDto(user, roles);
        }

        public async Task<IdentityUserDto?> FindByIdWithDetailsAsync(Guid userId)
        {
            var user = await _userManager.Users
                .Include(u => u.UserDetails)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return MapToDto(user, roles);
        }

        #endregion

        #region User Creation

        public async Task<Result<IdentityUserDto>> CreateUserAsync(string email, string userName, string password, string firstName, string? lastName)
        {
            var user = new ApplicationUser
            {
                Email = email,
                UserName = userName,
                EmailConfirmed = true,
                UserDetails = new UserDetails
                {
                    FirstName = firstName,
                    LastName = lastName ?? string.Empty
                },
                RefreshTokens = new List<RefreshToken>()
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<IdentityUserDto>.Failure($"Failed to create user: {errors}");
            }

            return Result<IdentityUserDto>.Success(data: MapToDto(user, new List<string>()));
        }

        #endregion

        #region Password Operations

        public async Task<Result> CheckPasswordSignInAsync(Guid userId, string password)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Result.Failure("User not found.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return Result.Failure("Account is locked out. Please try again later.");

            if (!result.Succeeded)
                return Result.Failure("Invalid email or password.");

            return Result.Success();
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Result.Failure("User not found.");

            var changeResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!changeResult.Succeeded)
            {
                var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to change password: {errors}");
            }

            return Result.Success();
        }

        #endregion

        #region Role Operations

        public async Task<IList<string>> GetRolesAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return new List<string>();

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<Result> AddToRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Result.Failure("User not found.");

            var result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to assign role: {errors}");
            }

            return Result.Success();
        }

        public async Task<Result> RemoveFromRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Result.Failure("User not found.");

            var result = await _userManager.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to remove role: {errors}");
            }

            return Result.Success();
        }

        public async Task<bool> IsInRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            return await _userManager.IsInRoleAsync(user, role);
        }

        public async Task<bool> RoleExistsAsync(string role)
        {
            return await _dbContext.Roles
                .AnyAsync(r => r.NormalizedName == role.ToUpperInvariant());
        }

        #endregion

        #region User Updates

        public async Task<Result> UpdateUserStatusAsync(Guid userId, int status)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Result.Failure("User not found.");

            user.Status = status;
            await _userManager.UpdateAsync(user);

            return Result.Success();
        }

        public async Task<Result> UpdateProfileAsync(Guid userId, string firstName, string? lastName, string? imageKey)
        {
            var user = await _userManager.Users
                .Include(u => u.UserDetails)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Result.Failure("User not found.");

            user.UserDetails.FirstName = firstName;
            user.UserDetails.LastName = lastName ?? string.Empty;
            user.UserDetails.ImageKey = imageKey;

            await _dbContext.SaveChangesAsync();

            return Result.Success();
        }

        #endregion

        #region Duplicate Checks

        public async Task<DuplicateCheckResult?> CheckDuplicateAsync(string email, string userName)
        {
            var normalizedEmail = _userManager.NormalizeEmail(email);
            var normalizedUserName = _userManager.NormalizeName(userName);

            var existingUser = await _userManager.Users
                .Where(u => u.NormalizedEmail == normalizedEmail
                          || u.NormalizedUserName == normalizedUserName)
                .Select(u => new { u.NormalizedEmail, u.NormalizedUserName })
                .FirstOrDefaultAsync();

            if (existingUser == null) return null;

            return new DuplicateCheckResult
            {
                IsDuplicateEmail = existingUser.NormalizedEmail == normalizedEmail,
                IsDuplicateUserName = existingUser.NormalizedUserName == normalizedUserName
            };
        }

        #endregion

        #region Query

        public async Task<(List<IdentityUserDto> Users, int TotalCount)> GetFilteredUsersAsync(
            string? searchTerm, int? status, string? role, int page, int pageSize)
        {
            var query = _userManager.Users
                .Include(u => u.UserDetails)
                .AsQueryable();

            // Filter by status
            if (status.HasValue)
                query = query.Where(u => u.Status == status.Value);
            else
                query = query.Where(u => u.Status != (int)EntityStatus.Deleted);

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(u =>
                    u.Email!.ToLower().Contains(term) ||
                    u.UserName!.ToLower().Contains(term) ||
                    u.UserDetails.FirstName.ToLower().Contains(term));
            }

            // Filter by role
            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleEntity = await _dbContext.Roles
                    .FirstOrDefaultAsync(r => r.NormalizedName == role.ToUpperInvariant());

                if (roleEntity != null)
                {
                    var userIdsInRole = _dbContext.UserRoles
                        .Where(ur => ur.RoleId == roleEntity.Id)
                        .Select(ur => ur.UserId);

                    query = query.Where(u => userIdsInRole.Contains(u.Id));
                }
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Load roles for each user
            var userDtos = new List<IdentityUserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(MapToDto(user, roles));
            }

            return (userDtos, totalCount);
        }

        #endregion

        #region Private Helpers

        private static IdentityUserDto MapToDto(ApplicationUser user, IList<string> roles)
        {
            return new IdentityUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                FirstName = user.UserDetails?.FirstName ?? string.Empty,
                LastName = user.UserDetails?.LastName,
                ImageKey = user.UserDetails?.ImageKey,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            };
        }

        #endregion
    }
}
