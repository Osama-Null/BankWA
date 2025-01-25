using BankWA.Data;
using BankWA.Models.ViewModels;
using BankWA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

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
            ViewBag.Roles = new SelectList(_roleManager.Roles, "RoleId", "Name");
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

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var transactions = await _context.Transactions.Where(t => t.UserId == user.Id).ToListAsync();

            return View(transactions);
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
        public async Task<IActionResult> Deposit(string? id)
        {
            if (id == null || string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Deposit");
            }

            var acco = await _userManager.FindByIdAsync(id);
            if (acco == null || string.IsNullOrEmpty(acco.Email))
            {
                return RedirectToAction("Deposit");
            }
            TransactionViewModel model = new TransactionViewModel
            {
                UserId = acco.Id,
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

            return RedirectToAction("AccountDetails", new { id = model.UserId });
        }

        //==================WITHDRAW==================\\
        public IActionResult Withdraw()
        {
            return View();
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
                return RedirectToAction("Account");
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

            return RedirectToAction("AccountDetails", new { id = model.UserId });
        }

        //==================TRANSFER==================\\
        public IActionResult Transfer()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Transfer(TransferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var sender = await _userManager.FindByIdAsync(model.SenderId);
            var receiver = await _userManager.FindByIdAsync(model.ReceiverId);
            if (sender == null || receiver == null)
            {
                return RedirectToAction("Account");
            }
            if (sender.Balance < model.Amount)
            {
                ModelState.AddModelError("", "Insufficient balance.");
                return View(model);
            }
            sender.Balance -= model.Amount;
            receiver.Balance += model.Amount;

            var transaction = new Transaction
            {
                UserId = model.SenderId,
                Amount = -model.Amount,
                Date = DateTime.UtcNow,
                Type = TransactionType.Transfer,
                ReceiverId = model.ReceiverId
            };

            await _context.Transactions.AddAsync(transaction);
            _context.Users.Update(sender);
            _context.Users.Update(receiver);
            await _context.SaveChangesAsync();

            return RedirectToAction("AccountDetails", new { id = model.SenderId });
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
