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
                var user = await _userManager.FindByEmailAsync(model.Email);
                if(user != null)
                {
                    // Generate password reset token
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    
                    // Create the reset password link
                    var resetLink = Url.Action(
                        "ResetPassword", 
                        "Account", 
                        new { email = user.Email, token = token }, 
                        Request.Scheme  // This ensures it's a full URL with http/https
                    );
                    
                    // Send email with the link
                    var response = await _fluentEmail
                        .To(user.Email)
                        .Subject("Reset Your Password")
                        .Body($@"Click the link below to reset your password:{resetLink}")
                        .SendAsync();
                    
                    // Always show success message (security best practice - don't reveal if email exists)
                    TempData["Success"] = "If an account exists with that email, a password reset link has been sent.";
                    return RedirectToAction("VerifyEmail");
                }
                else
                {
                    // Don't reveal that the email doesn't exist (security best practice)
                    TempData["Success"] = "If an account exists with that email, a password reset link has been sent.";
                    return RedirectToAction("VerifyEmail");
                }
            }
            return View(model);
        }

        // NEW: ResetPassword GET - displays the form when user clicks email link
        public IActionResult ResetPassword(string email, string token)
        {
            if(string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid password reset link.";
                return RedirectToAction("Login", "Account");
            }
            
            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };
            
            return View(model);
        }

        // NEW: ResetPassword POST - processes the password reset
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if(user == null)
                {
                    // Don't reveal that the user doesn't exist
                    TempData["Success"] = "Password has been reset successfully.";
                    return RedirectToAction("Login", "Account");
                }
                
                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                
                if(result.Succeeded)
                {
                    TempData["Success"] = "Password has been reset successfully. Please login with your new password.";
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
            
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
