using System.Diagnostics;
using FluentEmail.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SocialMediaApp.Models;
using SocialMediaApp.ViewModels;

namespace SocialMediaApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly UserManager<Users> _userManager;
        private readonly IFluentEmail _fluentEmail;

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, IFluentEmail fluentEmail)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _fluentEmail = fluentEmail;
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if(ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if(result.Succeeded)
                {
                    if(!await _userManager.IsEmailConfirmedAsync(await _userManager.FindByEmailAsync(model.Email)))
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError("", "You need to confirm your email before logging in.");
                        return View(model);
                    }
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
                }
            }
            return View(model);
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if(ModelState.IsValid)
            {
                Users users = new Users
                {
                    FullName = model.Name,
                    Email = model.Email,
                    UserName = model.Email
                };

                var result = await _userManager.CreateAsync(users, model.Password);

                if(result.Succeeded)
                {
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(users);
                    FluentEmail.Core.Models.SendResponse response = await _fluentEmail
                        .To(users.Email)
                        .Subject("Confirm your email")
                        .Body($"Email confirmation code: {code}")
                        .SendAsync();
    
                    return RedirectToAction("EmailConfirmation", "Account", new { email = users.Email});
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }
            }
            return View(model);
        }

        public ActionResult EmailConfirmation(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register", "Account");
            }

            var model = new EmailConfirmationViewModel
            {
                Email = email,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EmailConfirmation(EmailConfirmationViewModel model)
        {
            if(string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Code))
            {
                //bad request
                return RedirectToAction("Index", "Home");
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if(user == null)
            {
                //bad request
                return RedirectToAction("Index", "Home");
            }
            var result = await _userManager.ConfirmEmailAsync(user, model.Code);
            if(result.Succeeded)            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                //something went wrong
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Email);
                if(user != null)
                {
                    return RedirectToAction("ChangePassword", "Account", new{ username = user.UserName });
                }
                else
                {
                    ModelState.AddModelError("", "Something went wrong.");
                    return View(model);
                }
            }
            return View(model);
        }


        public IActionResult ChangePassword(string username)
        {
            if(string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View(new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if(user != null)
                {
                    var result = await _userManager.RemovePasswordAsync(user);

                    if(result.Succeeded)
                    {
                        result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "User not found.");
                    return View(model);
                }
            }
            {
                
                ModelState.AddModelError("", "Something went wrong.");
                return View(model);
            }
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
