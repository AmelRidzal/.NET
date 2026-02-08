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

        // Constructor injection for SignInManager, UserManager, and IFluentEmail
        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, IFluentEmail fluentEmail)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _fluentEmail = fluentEmail;
        }

        // GET: /Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if(ModelState.IsValid)
            {
                // Attempt to sign in the user
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if(result.Succeeded)
                {
                    // Check if the user's email is confirmed before allowing them to proceed
                    if(!await _userManager.IsEmailConfirmedAsync(await _userManager.FindByEmailAsync(model.Email)))
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError("", "You need to confirm your email before logging in.");
                        return View(model);
                    }
                    return RedirectToAction("Index", "Home", new { message = "Login successful!" });
                }
                else
                {
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: /Account/Register
        public ActionResult Register()
        {
            return View();
        }
        
        // POST: /Account/Register
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

                // Create the user with the specified password
                var result = await _userManager.CreateAsync(users, model.Password);

                if(result.Succeeded)
                {
                    // Generate email confirmation token and send email
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
                    TempData["Error"] = "Something went wrong!";
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: /Account/EmailConfirmation
        public ActionResult EmailConfirmation(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register", "Account", new { message = "Email is required for confirmation." });
            }
            // Pass the email to the view so it can be used in the form
            var model = new EmailConfirmationViewModel
            {
                Email = email,
            };

            return View(model);
        }

        // POST: /Account/EmailConfirmation
        [HttpPost]
        public async Task<IActionResult> EmailConfirmation(EmailConfirmationViewModel model)
        {
            // Validate the input
            if(string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Code))
            {
                //bad request
                return RedirectToAction("Index", "Home", new { message = "Invalid email confirmation request." });
            }
            // Find the user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if(user == null)
            {
                //bad request
                return RedirectToAction("Index", "Home", new { message = "Invalid email confirmation request." });
            }
            // Confirm the email using the provided code
            var result = await _userManager.ConfirmEmailAsync(user, model.Code);
            if(result.Succeeded)            {
                return RedirectToAction("Login", "Account", new { message = "Email confirmed successfully. You can now log in." });
            }
            else
            {
                //something went wrong
                return RedirectToAction("Index", "Home", new { message = "Email confirmation failed. Please try again." });
            }
        }

        // GET: /Account/VerifyEmail
        public ActionResult VerifyEmail()
        {
            return View();
        }

        // POST: /Account/VerifyEmail
        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if(ModelState.IsValid)
            {
                // Find the user by email
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
                    
                   return RedirectToAction("VerifyEmail", "Account", new { message = "If an account with that email exists, a password reset link has been sent." });
                }
                else
                {
                    return RedirectToAction("VerifyEmail", "Account", new { message = "If an account with that email exists, a password reset link has been sent." });
                }
            }
            TempData["Error"] = "Please enter a valid email address.";
            return View(model);
        }

        // GET: /Account/ResetPassword
        public IActionResult ResetPassword(string email, string token)
        {
            if(string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account", new { message = "Invalid password reset request." });
            }
            // Pass the email and token to the view so they can be used in the form
            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };
            TempData["Error"] = "Please enter a new password.";
            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if(ModelState.IsValid)
            {
                // Find the user by email
                var user = await _userManager.FindByEmailAsync(model.Email);
                if(user == null)
                {
                    return RedirectToAction("Login", "Account", new { message = "If an account with that email exists, the password has been reset." });
                }
                
                // Reset the password using the provided token and new password
                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                
                if(result.Succeeded)
                {
                    return RedirectToAction("Login", "Account", new { message = "Password reset successful. You can now log in with your new password." });
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    TempData["Error"] = "Something went wrong! Please try again.";
                    return View(model);
                }
            }
            TempData["Error"] = "Please enter a valid password.";
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home", new { message = "You have been logged out." });
        }
    }
}
