using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SocialMediaApp.Models;
using SocialMediaApp.ViewModels;

namespace SocialMediaApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly SignInManager<Users> _signInManager;

        // Constructor to inject UserManager and IWebHostEnvironment
        public ProfileController(UserManager<Users> userManager, IWebHostEnvironment webHostEnvironment, SignInManager<Users> signInManager)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _signInManager = signInManager;
        }

        // GET: /Profile
        public async Task<IActionResult> Profile()
        {
            // Get the currently logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { message = "Please log in to view your profile." });
            }
            
            // Create a view model to pass user data to the view
            var model = new ProfileViewModel
            {
                Name = user.FullName,
                Email = user.Email ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                ProfilePictureUrl = user.ProfileImagePath,
                ExistingImagePath = user.ProfileImagePath
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Get the currently logged-in user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { message = "Please log in to update your profile." });
                }

                // Update basic info
                user.FullName = model.Name;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.DateOfBirth = model.DateOfBirth;

                // Handle profile picture upload
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    // Delete old profile picture if exists
                    if (!string.IsNullOrEmpty(user.ProfileImagePath))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfileImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath) && !user.ProfileImagePath.Contains("default-avatar"))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Save new profile picture
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                    
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    // Generate a unique file name to prevent overwriting
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save the file to the server
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImage.CopyToAsync(fileStream);
                    }

                    // Update user's profile image path
                    user.ProfileImagePath = "/images/profiles/" + uniqueFileName;
                }

                // Update the user in the database
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["Success"] = "Profile updated successfully!";
                    return RedirectToAction("Profile", "Profile", new { message = "Profile updated successfully!" });
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    TempData["Error"] = "Something went wrong! Please try again.";
                    return View("Profile", model);
                }
            }
            TempData["Error"] = "Please fill in all required fields correctly.";
            return View("Profile", model);
        }
    
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.OldPassword,
                model.NewPassword
            );

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
            TempData["Success"] = "Password changed successfully. Please log in again.";

            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");

        }
    
    }
}