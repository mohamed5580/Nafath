using Domin.Entity;
using Infrastructure.Models;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Domin.Resource;
using Infrastructure.Models;
using Infrastructure.Data;
using Infrastructure.Models.ViewModel;

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
        [Authorize(Roles = "Admin,User")]
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
                return RedirectToAction("Roles");
            }

            // If model is INVALID, return to view with errors
            model.Roles = _roleManager.Roles.OrderBy(x => x.Name).ToList();
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRole(string Id)
        {
            var role = _roleManager.Roles.FirstOrDefault(x => x.Id == Id);
            if ((await _roleManager.DeleteAsync(role)).Succeeded)
                return RedirectToAction(nameof(Roles));

            return RedirectToAction("Roles");
        }

       

        [Authorize(Roles = "Admin,User")]
        public IActionResult Registers()
        {
            var model = new RegisterViewModel
            {
                NewRegister = new NewRegister(),
                Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList(),
                Users = _context.VwUsers.OrderBy(u => u.Role).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Registers(RegisterViewModel model)
        {
            // Always repopulate Roles & Users when re-displaying form
            model.Roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
            model.Users = _context.VwUsers.OrderBy(u => u.Role).ToList();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Handle file upload
            var file = HttpContext.Request.Form.Files.FirstOrDefault();
            if (file != null)
            {
                var imageName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var savePath = Path.Combine("wwwroot", Helper.PathSaveImageuser, imageName);
                using var stream = new FileStream(savePath, FileMode.Create);
                await file.CopyToAsync(stream);
                model.NewRegister.ImageUser = imageName;
            }

            var isNew = string.IsNullOrEmpty(model.NewRegister.Id);
            ApplicationUser user;

            if (isNew)
            {
                // Create new user
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    FullName = model.NewRegister.Name,
                    UserName = model.NewRegister.Email,
                    Email = model.NewRegister.Email,
                    AcceptTerms = model.NewRegister.ActiveUser,
                    AvatarUrl = model.NewRegister.ImageUser
                };

                var result = await _userManager.CreateAsync(user, model.NewRegister.Password);
                if (result.Succeeded)
                {
                    var roleResult = await _userManager.AddToRoleAsync(user, model.NewRegister.RoleName);
                    if (roleResult.Succeeded)
                        SessionMsg(Helper.Success, ResourceWeb.lbSave, ResourceWeb.lbNotSavedMsgUserRole);
                    else
                        SessionMsg(Helper.Error, ResourceWeb.lbNotSaved, ResourceWeb.lbNotSavedMsgUser);
                }
                else
                {
                    SessionMsg(Helper.Error, ResourceWeb.lbNotSaved, ResourceWeb.lbNotUpdateMsgUser);
                }
            }
            else
            {
                // Update existing user
                user = await _userManager.FindByIdAsync(model.NewRegister.Id);
                if (user == null)
                {
                    SessionMsg(Helper.Error, ResourceWeb.lbMsgErrorLogin, ResourceWeb.lbMsgErrorLogin);
                    return RedirectToAction("Registers");
                }

                user.FullName = model.NewRegister.Name;
                user.UserName = model.NewRegister.Email;
                user.Email = model.NewRegister.Email;
                user.AcceptTerms = model.NewRegister.ActiveUser;
                user.AvatarUrl = model.NewRegister.ImageUser;

                var updateResult = await _userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    var oldRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, oldRoles);
                    var addRoleResult = await _userManager.AddToRoleAsync(user, model.NewRegister.RoleName);
                    if (addRoleResult.Succeeded)
                        SessionMsg(Helper.Success, ResourceWeb.lbUpdate, ResourceWeb.lbNotUpdateMsgUserRole);
                    else
                        SessionMsg(Helper.Error, ResourceWeb.lbNotUpdate, ResourceWeb.lbNotUpdateMsgUserRole);
                }
                else
                {
                    SessionMsg(Helper.Error, ResourceWeb.lbNotUpdate, ResourceWeb.lbNotUpdateMsgUser);
                }
            }

            return RedirectToAction("Registers");
        }

        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin,User")]
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
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var Result = await _signInManager.PasswordSignInAsync(model.Eamil,
                    model.Password, model.RememberMy, false);
                if (Result.Succeeded)
                    return RedirectToAction("Index", "Home");
                else
                    ViewBag.ErrorLogin = false;
            }
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
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
