using Domin.Entity;
using Domin.Resource;
using Infrastructure.Data;
using Infrastructure.Models;
using Infrastructure.Models.ViewModel;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Nafath.Areas.Admin.Controllers
{

    [Area("Admin")]

    public class AccountsController : Controller
    {
        #region Declaration
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private const string DefaultAvatarFile = "user1.jpg";
        #endregion


        #region Constructor
        public AccountsController(RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _environment = environment;
        }
        #endregion

        #region Method

        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Roles()
        {
            return View(new RolesViewModel
            {
                NewRole = new NewRole(),
                Roles = _roleManager.Roles.OrderBy(x => x.Name).ToList()
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Roles(RolesViewModel model)
        {
            if (ModelState.IsValid) // CORRECTED: Check for VALID model state
            {
                if (model.NewRole.RoleId == null)
                {
                    // Create new role
                    var result = await _roleManager.CreateAsync(new IdentityRole(model.NewRole.RoleName));

                    if (result.Succeeded)
                        SessionMsg(Helper.Success, ResourceWeb.lbSave, ResourceWeb.lbSaveMsgRole);
                    else
                        SessionMsg(Helper.Error, ResourceWeb.lbNotSaved, ResourceWeb.lbNotSavedMsgRole);
                }
                else // Update existing role
                {
                    var roleUpdate = await _roleManager.FindByIdAsync(model.NewRole.RoleId);
                    if (roleUpdate == null)
                    {
                        ModelState.AddModelError("", ResourceWeb.lbNotSaved);
                    }
                    else
                    {
                        roleUpdate.Name = model.NewRole.RoleName;
                        var result = await _roleManager.UpdateAsync(roleUpdate);

                        if (result.Succeeded)
                            SessionMsg(Helper.Success, ResourceWeb.lbUpdate, ResourceWeb.lbUpdateMsgRole);
                        else
                            SessionMsg(Helper.Error, ResourceWeb.lbNotUpdate, ResourceWeb.lbNotUpdateMsgRole);
                    }
                }
                SessionMsg(Helper.Error, ResourceWeb.lbNotUpdate, ResourceWeb.lbNotUpdateMsgRole);

                return RedirectToAction("Roles");
            }

            // If model is INVALID, return to view with errors
            model.Roles = _roleManager.Roles.OrderBy(x => x.Name).ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole(string Id)
        {
            var role = _roleManager.Roles.FirstOrDefault(x => x.Id == Id);
            if ((await _roleManager.DeleteAsync(role)).Succeeded)
                return RedirectToAction(nameof(Roles));

            return RedirectToAction("Roles");
        }



        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Registers()
        {
            var model = new RegisterViewModel
            {
                NewRegister = new NewRegister(),
                Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList(),
                Users = await LoadUsersAsync()
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Registers(RegisterViewModel model)
        {
            /* PSEUDOCODE / PLAN (detailed)
            1. Validate incoming model:
               - If ModelState is INVALID -> populate model.Roles and model.Users and return View(model) so validation errors show.
            2. Handle uploaded file:
               - If files present, save first file to configured path with GUID filename and set model.NewRegister.ImageUser to the filename.
               - If no file:
                 - If editing existing user (Id provided) -> load existing user's AvatarFile and use it (fallback to default).
                 - Else (creating) -> set default avatar path.
            3. For creating a new user:
               - Ensure required fields (Password and RoleName) are present; if not, add ModelState errors and return View with repopulated lists.
               - Create ApplicationUser and call _userManager.CreateAsync(user, password).
               - If creation fails, add Identity errors to ModelState, repopulate lists and return View.
               - If creation succeeds, ensure roleName exists in RoleManager; then call AddToRoleAsync and set SessionMsg accordingly.
            4. For updating existing user:
               - Load user by Id; if not found, add ModelState error and return View with lists.
               - Update properties and call UpdateAsync.
               - If update fails, push errors to ModelState and return.
               - If update succeeds, remove old roles, validate new role exists, then AddToRoleAsync and set SessionMsg accordingly.
            5. Always when returning view after failure, ensure model.Roles and model.Users are populated (using LoadUsersAsync).
            6. Redirect to Registers on success.
            7. Use null checks before calling APIs that don't accept null (passwords and role names).
            */
            // 1. Validate model first
            if (!ModelState.IsValid)
            {
                model.Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
                model.Users = await LoadUsersAsync();
                SessionMsg(Helper.Error, ResourceWeb.lbNotSaved, ResourceWeb.lbNotSavedMsgUser);

                return View(model);
            }

            var uploadedAvatarFile = await SaveAvatarAsync(model.NewRegister.ImageFile);
            if (!string.IsNullOrWhiteSpace(uploadedAvatarFile))
                model.NewRegister.ImageUser = uploadedAvatarFile;


            // 3. Create new user
            if (string.IsNullOrEmpty(model.NewRegister.Id))
            {
                // Validate required fields for creation
                if (string.IsNullOrWhiteSpace(model.NewRegister.Password))
                {
                    ModelState.AddModelError("", ResourceWeb.lbNotSaved + ": " + "Password is required.");
                    model.Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
                    model.Users = await LoadUsersAsync();
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.NewRegister.RoleName))
                {
                    ModelState.AddModelError("", ResourceWeb.lbNotSaved + ": " + "Role is required.");
                    model.Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
                    model.Users = await LoadUsersAsync();
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    FullName = model.NewRegister.FullName,
                    UserName = model.NewRegister.Email,
                    Email = model.NewRegister.Email,
                    AcceptTerms = model.NewRegister.ActiveUser,
                    AvatarFile = NormalizeStoredAvatar(model.NewRegister.ImageUser)
                };
                var exist = await _userManager.FindByEmailAsync(model.NewRegister.Email!);

                if (exist != null)
                {
                    ModelState.AddModelError("", "البريد الإلكتروني مستخدم بالفعل");

                    model.Roles = _roleManager.Roles.OrderBy(x => x.Name).ToList();
                    model.Users = await LoadUsersAsync();

                    return View(model);
                }

                var result = await _userManager.CreateAsync(user, model.NewRegister.Password!);
                if (result.Succeeded)
                {
                    var roleName = model.NewRegister.RoleName!;
                    if (await _roleManager.RoleExistsAsync(roleName))
                    {
                        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
                        if (roleResult.Succeeded)
                            SessionMsg(Helper.Success, ResourceWeb.lbSave, ResourceWeb.lbNotSavedMsgUserRole);
                        else
                            SessionMsg(Helper.Error, ResourceWeb.lbNotSaved, ResourceWeb.lbNotSavedMsgUser);
                    }
                    else
                    {
                        // Role doesn't exist
                        ModelState.AddModelError("", ResourceWeb.lbNotSaved + ": " + "Selected role does not exist.");
                        model.Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
                        model.Users = await LoadUsersAsync();
                        return View(model);
                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);

                    model.Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
                    model.Users = await LoadUsersAsync();
                    return View(model);
                }
            }
            else
            {
                // 4. Update existing user
                var userUpdate = await _userManager.FindByIdAsync(model.NewRegister.Id);
                if (userUpdate == null)
                {
                    ModelState.AddModelError("", "المستخدم غير موجود");
                    model.Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
                    model.Users = await LoadUsersAsync();
                    return View(model);
                }
                var oldAvatarFile = userUpdate.AvatarFile;
                var newAvatarFile = string.IsNullOrWhiteSpace(model.NewRegister.ImageUser)
                    ? oldAvatarFile
                    : model.NewRegister.ImageUser;

                if (!string.Equals(userUpdate.Email, model.NewRegister.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var existingEmail = await _userManager.FindByEmailAsync(model.NewRegister.Email!);
                    if (existingEmail != null && existingEmail.Id != userUpdate.Id)
                    {
                        ModelState.AddModelError("", "البريد الإلكتروني مستخدم بالفعل");
                        model.Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
                        model.Users = await LoadUsersAsync();
                        return View(model);
                    }
                }

                userUpdate.FullName = model.NewRegister.FullName;
                userUpdate.UserName = model.NewRegister.Email;
                userUpdate.Email = model.NewRegister.Email;
                userUpdate.AcceptTerms = model.NewRegister.ActiveUser;
                userUpdate.AvatarFile = NormalizeStoredAvatar(newAvatarFile);

                var result = await _userManager.UpdateAsync(userUpdate);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrWhiteSpace(uploadedAvatarFile) &&
                        !string.Equals(oldAvatarFile, userUpdate.AvatarFile, StringComparison.OrdinalIgnoreCase))
                    {
                        DeleteStoredAvatar(oldAvatarFile);
                    }

                    var oldRoles = await _userManager.GetRolesAsync(userUpdate);
                    if (oldRoles.Any())
                        await _userManager.RemoveFromRolesAsync(userUpdate, oldRoles);

                    if (string.IsNullOrWhiteSpace(model.NewRegister.RoleName))
                    {
                        ModelState.AddModelError("", ResourceWeb.lbNotUpdate + ": " + "Role is required.");
                        model.Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
                        model.Users = await LoadUsersAsync();
                        return View(model);
                    }

                    var roleName = model.NewRegister.RoleName!;
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        ModelState.AddModelError("", ResourceWeb.lbNotUpdate + ": " + "Selected role does not exist.");
                        model.Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
                        model.Users = await LoadUsersAsync();
                        return View(model);
                    }

                    var addRole = await _userManager.AddToRoleAsync(userUpdate, roleName);
                    if (addRole.Succeeded)
                        SessionMsg(Helper.Success, ResourceWeb.lbUpdate, ResourceWeb.lbNotUpdateMsgUserRole);
                    else
                        SessionMsg(Helper.Error, ResourceWeb.lbNotUpdate, ResourceWeb.lbNotUpdateMsgUserRole);
                }
                else
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);

                    model.Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
                    model.Users = await LoadUsersAsync();
                    return View(model);
                }
            }

            return RedirectToAction("Registers", "Accounts");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        // دالة مساعدة لتحميل جميع المستخدمين من UserManager
        private async Task<List<VwUsers>> LoadUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<VwUsers>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new VwUsers
                {
                    Id = user.Id,
                    Name = user.FullName ?? "",
                    Email = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "No Role",
                    AcceptTerms = user.AcceptTerms ?? false,
                    AvatarFile = NormalizeStoredAvatar(user.AvatarFile)
                });
            }

            return userList.OrderBy(u => u.Role).ToList();
        }

  

        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToAction("Registers", "Accounts");

            var avatarFile = user.AvatarFile;
            if ((await _userManager.DeleteAsync(user)).Succeeded)
                DeleteStoredAvatar(avatarFile);

            return RedirectToAction("Registers", "Accounts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = nameof(RegisterViewModel.ChangePassword))] ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Id))
            {
                SessionMsg(Helper.Error, ResourceWeb.lbNotSaved, ResourceWeb.lbMsgNotSavedChangePassword);
                return RedirectToAction(nameof(Registers));
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user != null)
            {
                await _userManager.RemovePasswordAsync(user);
                var AddNewPassword = await _userManager.AddPasswordAsync(user, model.NewPassword);
                if (AddNewPassword.Succeeded)
                    SessionMsg(Helper.Success, ResourceWeb.lbSave, ResourceWeb.lbMsgSavedChangePassword);
                else
                    SessionMsg(Helper.Error, ResourceWeb.lbNotSaved, ResourceWeb.lbMsgNotSavedChangePassword);

                return RedirectToAction(nameof(Registers));
            }

            return RedirectToAction(nameof(Registers));

        }

        [AllowAnonymous]
        public IActionResult Login()
        {
      
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
       
            string returnUrl = Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            
            string login = model.Email;
            if (new EmailAddressAttribute().IsValid(model.Email))
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    login = user.UserName;
                }
            }

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(login, model.Password, model.RememberMy, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    ModelState.AddModelError("", $"فشل تسجيل الدخول: {result}");
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = model.RememberMy });
                }
                if (result.IsLockedOut)
                {
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
                if (result.Succeeded)
                {

                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                ModelState.AddModelError("", $"فشل تسجيل الدخول: {result}");
                return View(model);
            }

            return View(model);


        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        private async Task<string?> SaveAvatarAsync(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var imageName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            var folder = Path.Combine(_environment.WebRootPath, Helper.PathSaveImageuser);

            Directory.CreateDirectory(folder);

            await using var stream = new FileStream(Path.Combine(folder, imageName), FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return imageName;
        }

        private static string NormalizeStoredAvatar(string? avatarFile)
        {
            return string.IsNullOrWhiteSpace(avatarFile)
                ? DefaultAvatarFile
                : avatarFile.Trim();
        }

        private void DeleteStoredAvatar(string? avatarFile)
        {
            var fileName = GetAdminAvatarFileName(avatarFile);
            if (string.IsNullOrWhiteSpace(fileName) || IsDefaultAvatar(fileName))
                return;

            var path = Path.Combine(_environment.WebRootPath, Helper.PathSaveImageuser, fileName);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        private static string? GetAdminAvatarFileName(string? avatarFile)
        {
            if (string.IsNullOrWhiteSpace(avatarFile))
                return null;

            var value = avatarFile.Trim();
            if (value.StartsWith("~/", StringComparison.Ordinal))
                value = value[1..];

            if (value.StartsWith(Helper.PathImageuser, StringComparison.OrdinalIgnoreCase))
                return Path.GetFileName(value.Replace('/', Path.DirectorySeparatorChar));

            if (value.StartsWith("/", StringComparison.Ordinal) ||
                Uri.TryCreate(value, UriKind.Absolute, out _))
            {
                return null;
            }

            return Path.GetFileName(value);
        }

        private static bool IsDefaultAvatar(string fileName)
        {
            return string.Equals(fileName, DefaultAvatarFile, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(fileName, "user1.png", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(fileName, "Defult.png", StringComparison.OrdinalIgnoreCase);
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
        #endregion
    }
}
