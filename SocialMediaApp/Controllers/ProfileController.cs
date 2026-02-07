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

        public ProfileController(UserManager<Users> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

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
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
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

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImage.CopyToAsync(fileStream);
                    }

                    user.ProfileImagePath = "/images/profiles/" + uniqueFileName;
                }

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["Success"] = "Profile updated successfully!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View("Profile", model);
                }
            }

            return View("Profile", model);
        }
    }
}