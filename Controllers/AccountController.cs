using BankWA.Data;
using BankWA.Models.ViewModels;
using BankWA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;

namespace BankWA.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        #region InjectedServices
        private UserManager<AppUser> _userManager;
        private SignInManager<AppUser> _signInManager;
        private RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> SignInManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = SignInManager;
            _roleManager = roleManager;
            _context = context;
        }
        #endregion
        public IActionResult Index()
        {
            return View();
        }
        [AllowAnonymous]
        public IActionResult Login()
        {
            if(_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Profile");
            }
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("LoginError", "Invalid email or password.");
                return View(model);
            }
            return RedirectToAction("Profile");
        }
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Profile");
            }
            ViewBag.Roles = new SelectList(_roleManager.Roles, "RoleId", "Name");
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine(ModelState);
                ViewBag.Roles = new SelectList(_roleManager.Roles, "RoleId", "Name");
                return View(model);
            }

            AppUser user = new AppUser
            {
                Name = model.Name,
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Mobile,
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }
                return View(model);
            }
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Profile(string searchString, DateTime? startDate, DateTime? endDate, decimal? minAmount, decimal? maxAmount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var transactions = _context.Transactions.Include(t => t.Receiver).Where(t => t.UserId == user.Id);

            if (!string.IsNullOrEmpty(searchString))
            {
                transactions = transactions.Where(t => t.Amount.ToString().Contains(searchString) || t.Date.ToString("g").Contains(searchString));
            }

            if (startDate.HasValue)
            {
                transactions = transactions.Where(t => t.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                transactions = transactions.Where(t => t.Date <= endDate.Value);
            }

            if (minAmount.HasValue)
            {
                transactions = transactions.Where(t => t.Amount >= minAmount.Value);
            }

            if (maxAmount.HasValue)
            {
                transactions = transactions.Where(t => t.Amount <= maxAmount.Value);
            }

            var filteredTransactions = await transactions.ToListAsync();

            return View(filteredTransactions);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> LogoutPost()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        //==================DEPOSIT==================\\
        public async Task<IActionResult> Deposit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            TransactionViewModel model = new TransactionViewModel
            {
                UserId = user.Id,
                AmountVM = 0, // Initialize with 0 or any default value
                DateVM = DateTime.Now,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Deposit(TransactionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var account = await _userManager.FindByIdAsync(model.UserId);
            if (account == null)
            {
                return RedirectToAction("Account");
            }
            account.Balance += model.AmountVM;

            var transaction = new Transaction
            {
                UserId = model.UserId,
                Amount = model.AmountVM,
                Date = DateTime.UtcNow,
                Type = TransactionType.Deposit
            };

            await _context.Transactions.AddAsync(transaction);
            _context.Users.Update(account);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile");
        }


        //==================WITHDRAW==================\\
        public async Task<IActionResult> Withdraw()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            TransactionViewModel model = new TransactionViewModel
            {
                UserId = user.Id,
                AmountVM = 0, // Initialize with 0 or any default value
                DateVM = DateTime.Now,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Withdraw(TransactionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var account = await _userManager.FindByIdAsync(model.UserId);
            if (account == null)
            {
                return RedirectToAction("Profile");
            }
            if (account.Balance < model.AmountVM)
            {
                ModelState.AddModelError("", "Insufficient balance.");
                return View(model);
            }
            account.Balance -= model.AmountVM;

            var transaction = new Transaction
            {
                UserId = model.UserId,
                Amount = -model.AmountVM,
                Date = DateTime.UtcNow,
                Type = TransactionType.Withdraw
            };

            await _context.Transactions.AddAsync(transaction);
            _context.Users.Update(account);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile");
        }



        //==================TRANSFER==================\\
        public async Task<IActionResult> Transfer()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var users = await _userManager.Users.Where(u => u.Id != currentUser.Id).ToListAsync();
            ViewBag.ReceiverEmails = new SelectList(users, "Email", "Email");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Transfer(TransferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var users = await _userManager.Users.ToListAsync();
                ViewBag.ReceiverEmails = new SelectList(users, "Email", "Email");
                return View(model);
            }

            var sender = await _userManager.GetUserAsync(User);
            var receiver = await _userManager.FindByEmailAsync(model.ReceiverEmail);
            if (sender == null || receiver == null)
            {
                return RedirectToAction("Profile");
            }
            if (sender.Balance < model.Amount)
            {
                ModelState.AddModelError("", "Insufficient balance.");
                var users = await _userManager.Users.ToListAsync();
                ViewBag.ReceiverEmails = new SelectList(users, "Email", "Email");
                return View(model);
            }
            sender.Balance -= model.Amount;
            receiver.Balance += model.Amount;

            var transaction = new Transaction
            {
                UserId = sender.Id,
                Amount = -model.Amount,
                Date = DateTime.UtcNow,
                Type = TransactionType.Transfer,
                ReceiverId = receiver.Id
            };

            await _context.Transactions.AddAsync(transaction);
            _context.Users.Update(sender);
            _context.Users.Update(receiver);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile");
        }
        //=======================EDIT==================\\
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProfile(string? id)
        {
            if (id == null)
            {
                return RedirectToAction("Profile");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Email = user.Email,
                Mobile = user.PhoneNumber,
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> EditProfile(string id, EditProfileViewModel model)
        {
            if (id == null)
            {
                return RedirectToAction("Profile");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (!passwordCheck.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "*Invalid password");
                    return View(model);
                }
            }

            if (!string.IsNullOrEmpty(model.Mobile))
            {
                user.PhoneNumber = model.Mobile;
            }

            if (model.Img != null && model.Img.Length > 0)
            {
                var imageUrl = await ResizeImage(model.Img, user.Id);
                user.Img = imageUrl;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            return RedirectToAction("Profile");
        }


        private async Task<string> ResizeImage(IFormFile imageFile, string userId)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var fileName = $"{userId}_{Path.GetFileName(imageFile.FileName)}";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/css/Users/Images", fileName);

            using (var stream = new MemoryStream())
            {
                await imageFile.CopyToAsync(stream);
                using (var image = System.Drawing.Image.FromStream(stream))
                {
                    var resized = new System.Drawing.Bitmap(320, 320);
                    using (var graphics = System.Drawing.Graphics.FromImage(resized))
                    {
                        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.DrawImage(image, 0, 0, 320, 320);
                    }
                    resized.Save(filePath);
                }
            }

            return $"~/css/Users/Images/{fileName}";
        }

        //============================================\\
        //public string UploadFile(IFormFile Image)
        //{
        //    string uniqueFileName = null;

        //    if (Image != null)
        //    {
        //        string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "Images");
        //        uniqueFileName = Guid.NewGuid().ToString() + "_" + Image.FileName;
        //        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
        //        using (var fileStream = new FileStream(filePath, FileMode.Create))
        //        {
        //            Image.CopyTo(fileStream);
        //        }
        //    }
        //    return uniqueFileName;
        //}
    }
}