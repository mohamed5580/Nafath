using Domin.Entity;
using Domin.Resource;
using Infrastructure.Data;
using Infrastructure.Models;
using Infrastructure.Models.ViewModel;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly ApplicationDbContext _context;
        #endregion

        #region Constructor
        public AccountsController(RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }
        #endregion

        #region Method
        //[Authorize(Roles = "Admin,User")]
        public IActionResult Roles()
        {
            return View(new RolesViewModel
            {
                NewRole = new NewRole(),
                Roles = _roleManager.Roles.OrderBy(x => x.Name).ToList()
            });
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin,User")]
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

        //[Authorize(Roles = "Admin,User")]

        public async Task<IActionResult> DeleteRole(string Id)
        {
            var role = _roleManager.Roles.FirstOrDefault(x => x.Id == Id);
            if ((await _roleManager.DeleteAsync(role)).Succeeded)
                return RedirectToAction(nameof(Roles));

            return RedirectToAction("Roles");
        }



        //[Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Registers()
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
                    AvatarUrl = user.AvatarUrl ?? "user1.png"
                });
            }

            var model = new RegisterViewModel
            {
                NewRegister = new NewRegister(),
                Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList(),
                Users = userList.OrderBy(u => u.Role).ToList()
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Registers(RegisterViewModel model)
        {
            /* PSEUDOCODE / PLAN (detailed)
            1. Validate incoming model:
               - If ModelState is INVALID -> populate model.Roles and model.Users and return View(model) so validation errors show.
            2. Handle uploaded file:
               - If files present, save first file to configured path with GUID filename and set model.NewRegister.ImageUser to the filename.
               - If no file:
                 - If editing existing user (Id provided) -> load existing user's AvatarUrl and use it (fallback to default).
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

            // 2. Handle uploaded file
            var files = HttpContext.Request.Form.Files;
            if (files.Count > 0)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    string imageName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var saveFolder = Path.Combine("wwwroot", Helper.PathSaveImageuser);
                    if (!Directory.Exists(saveFolder))
                        Directory.CreateDirectory(saveFolder);

                    var filePath = Path.Combine(saveFolder, imageName);
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(fileStream);
                    model.NewRegister.ImageUser = imageName;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(model.NewRegister?.Id))
                {
                    var existingUser = await _userManager.FindByIdAsync(model.NewRegister.Id);
                    model.NewRegister.ImageUser = existingUser?.AvatarUrl ?? Path.Combine("Images", "user", "1.png");
                }
                else
                {
                    model.NewRegister.ImageUser = Path.Combine("Images", "user", "1.png");
                }
            }

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
                    AvatarUrl = model.NewRegister.ImageUser
                };

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

                userUpdate.FullName = model.NewRegister.FullName;
                userUpdate.UserName = model.NewRegister.Email;
                userUpdate.Email = model.NewRegister.Email;
                userUpdate.AcceptTerms = model.NewRegister.ActiveUser;
                userUpdate.AvatarUrl = model.NewRegister.ImageUser;

                var result = await _userManager.UpdateAsync(userUpdate);
                if (result.Succeeded)
                {
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
                    AvatarUrl = user.AvatarUrl ?? "user1.png"
                });
            }

            return userList.OrderBy(u => u.Role).ToList();
        }

        //[Authorize(Roles = "Admin,User")]

        public async Task<IActionResult> DeleteUser(string userId)
        {
            var User = _userManager.Users.FirstOrDefault(x => x.Id == userId);

            if (User.AvatarUrl != null && User.AvatarUrl != Guid.Empty.ToString())
            {
                var PathImage = Path.Combine(@"wwwroot/", Helper.PathImageuser, User.AvatarUrl);
                if (System.IO.File.Exists(PathImage))
                    System.IO.File.Delete(PathImage);
            }

            if ((await _userManager.DeleteAsync(User)).Succeeded)
                return RedirectToAction("Registers", "Accounts");

            return RedirectToAction("Registers", "Accounts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> ChangePassword(RegisterViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.ChangePassword.Id);
            if (user != null)
            {
                await _userManager.RemovePasswordAsync(user);
                var AddNewPassword = await _userManager.AddPasswordAsync(user, model.ChangePassword.NewPassword);
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
            SessionMsg(
                   Helper.Error,
                   ResourceWeb.lbNotSaved,
                   ResourceWeb.lbMsgNotSavedChangePassword
               );
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) 
                

                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "المستخدم غير موجود");
                return View(model);
            }

            // التحقق من كلمة المرور مباشرة
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);

            if (!passwordValid)
            {
                ModelState.AddModelError("", "فشل تسجيل الدخول:");
                return View(model);
            }
            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError("", "يجب تفعيل الحساب أولاً");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                    model.Email, // قد يحتاج إلى تغيير
                    model.Password,
                    model.RememberMy,
                    lockoutOnFailure: false
                );

            if (result.Succeeded)
            {

                return RedirectToAction("Roles", "Accounts", new { area = "Admin" });
            }

            ModelState.AddModelError("", $"فشل تسجيل الدخول: {result}");
            return View(model);
        }



        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
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
