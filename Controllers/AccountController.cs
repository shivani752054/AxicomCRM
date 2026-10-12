using AcxiomCRM.Models; using AcxiomCRM.Services; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Identity; using Microsoft.AspNetCore.Mvc;
namespace AcxiomCRM.Controllers;
public class AccountController : Controller {
  readonly SignInManager<AppUser> _sm; readonly UserManager<AppUser> _um; readonly AuditService _a;
  public AccountController(SignInManager<AppUser> sm, UserManager<AppUser> um, AuditService a) { _sm = sm; _um = um; _a = a; }
  [HttpGet] public IActionResult Login() => View();
  [HttpPost] public async Task<IActionResult> Login(LoginVm m) {
    if (!ModelState.IsValid) return View(m);
    var u = await _um.FindByEmailAsync(m.Email);
    if (u == null) { await _a.Log("FailedLogin", "User", null, null, m.Email); ModelState.AddModelError("", "Invalid email or password."); return View(m); }
    var r = await _sm.PasswordSignInAsync(u, m.Password, false, lockoutOnFailure: true);
    if (r.Succeeded) { await _a.Log("Login", "User", u.Id, userId: u.Id); return RedirectToAction("Index", "Home"); }
    await _a.Log(r.IsLockedOut ? "Lockout" : "FailedLogin", "User", u.Id, userId: u.Id);
    ModelState.AddModelError("", r.IsLockedOut ? "Account locked. Try again later." : "Invalid email or password."); return View(m);
  }
  [HttpGet] public IActionResult Register() => View();
  [HttpPost] public async Task<IActionResult> Register(RegisterVm m) {
    if (!ModelState.IsValid) return View(m);
    var u = new AppUser { UserName = m.Email, Email = m.Email, FullName = m.FullName };
    var r = await _um.CreateAsync(u, m.Password);   // Identity hashes the password
    if (!r.Succeeded) { foreach (var e in r.Errors) ModelState.AddModelError("", e.Description); return View(m); }
    await _um.AddToRoleAsync(u, "SalesExecutive"); await _a.Log("Register", "User", u.Id, userId: u.Id);
    await _sm.SignInAsync(u, false); return RedirectToAction("Index", "Home");
  }
  [HttpPost, Authorize] public async Task<IActionResult> Logout() { await _a.Log("Logout", "User"); await _sm.SignOutAsync(); return RedirectToAction("Login"); }
  public IActionResult Denied() => View();
}
