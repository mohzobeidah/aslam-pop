using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CampRegistrationApp.Models;

namespace CampRegistrationApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode)
    {
        var code = statusCode ?? 500;
        Response.StatusCode = code;

        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code,
            StatusText = code switch
            {
                404 => "غير موجود",
                403 => "ممنوع الوصول",
                401 => "غير مصرح",
                400 => "طلب خاطئ",
                _ => "خطأ في الخادم"
            }
        };

        model.Title = code switch
        {
            404 => "الصفحة غير موجودة!",
            403 => "لا تملك صلاحية الوصول!",
            401 => "غير مصرح بالدخول!",
            400 => "طلب غير صالح!",
            _ => "حدث خطأ غير متوقع!"
        };

        model.Message = code switch
        {
            404 => "عذراً، الصفحة التي تبحث عنها غير موجودة أو تم نقلها.",
            403 => "ليس لديك الصلاحية للوصول إلى هذه الصفحة. إذا كنت تعتقد أن هذا خطأ، يرجى التواصل مع المشرف.",
            401 => "يرجى تسجيل الدخول أولاً للوصول إلى هذه الصفحة.",
            400 => "الطلب الذي أرسلته غير صالح. يرجى التحقق من البيانات والمحاولة مرة أخرى.",
            _ => "نعتذر عن هذا الخلل. لقد تم إخطار الفريق التقني تلقائياً ونحن نعمل على حل المشكلة في أسرع وقت ممكن."
        };

        model.IconType = code switch
        {
            404 => "not-found",
            403 => "forbidden",
            401 => "unauthorized",
            400 => "bad-request",
            _ => "server-error"
        };

        model.ShowReportButton = code >= 500;

        return View(model);
    }
}
