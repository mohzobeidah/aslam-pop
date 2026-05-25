using System;
using System.Diagnostics;
using System.Text;
using CampRegistrationApp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CampRegistrationApp.Services
{
    static class RegistrationConstants
    {
        public const string MaritalStatusMarried = "متزوج";
        public const string RelationshipWife = "زوجة";
        public const string HealthStatusSick = "مريض";
    }

    public interface IRegistrationValidationService
    {
        bool ValidateRegistration(RegistrationViewModel model, ModelStateDictionary modelState, string viewName = "Index");
    }

    public class RegistrationValidationService : IRegistrationValidationService
    {
        static string Normalized(string? s) =>
            (s ?? string.Empty).Normalize(NormalizationForm.FormC).Trim();

        public bool ValidateRegistration(RegistrationViewModel model, ModelStateDictionary modelState, string viewName = "Index")
        {
            if (model == null) return false;

            if (model.Head == null)
            {
                modelState.AddModelError("", "بيانات رب الأسرة مفقودة");
                return false;
            }

            // 1. Marital Status Validation: married requires wife member
            var maritalStatus = Normalized(model.Head.MaritalStatus);
            if (maritalStatus.Contains(RegistrationConstants.MaritalStatusMarried, StringComparison.Ordinal) &&
                !model.Members.Any(m =>
                    Normalized(m.RelationshipToHead)
                        .Contains(RegistrationConstants.RelationshipWife, StringComparison.Ordinal)))
            {
                Debug.WriteLine($"[Validation] Married head without wife. Received MaritalStatus='{maritalStatus}'");
                modelState.AddModelError("", "بما أن الحالة الاجتماعية متزوج، يجب إضافة فرد بصفة زوجة");
                return false;
            }

            // 2. Health Status Validation for Head: sick requires disease or disability
            var healthStatus = Normalized(model.Head.HealthStatus);
            if (healthStatus.Contains(RegistrationConstants.HealthStatusSick, StringComparison.Ordinal) &&
                string.IsNullOrWhiteSpace(model.Head.ChronicDiseases) &&
                string.IsNullOrWhiteSpace(model.Head.DisabilityTypes))
            {
                Debug.WriteLine($"[Validation] Sick head without details. Received HealthStatus='{healthStatus}'");
                modelState.AddModelError("", "بما أن الحالة الصحية مريض، يجب اختيار مرض مزمن أو نوع إعاقة على الأقل لرب الأسرة");
                return false;
            }

            // 3. Health Status Validation for Members: sick requires disease or disability
            for (int i = 0; i < model.Members.Count; i++)
            {
                var m = model.Members[i];
                var memberHealth = Normalized(m.HealthStatus);
                if (memberHealth.Contains(RegistrationConstants.HealthStatusSick, StringComparison.Ordinal) &&
                    string.IsNullOrWhiteSpace(m.ChronicDiseases) &&
                    string.IsNullOrWhiteSpace(m.DisabilityTypes))
                {
                    Debug.WriteLine($"[Validation] Sick member #{i + 1} without details. Received HealthStatus='{memberHealth}'");
                    modelState.AddModelError("", $"الفرد رقم {i + 1}: بما أن الحالة الصحية مريض، يجب اختيار مرض مزمن أو نوع إعاقة على الأقل");
                    return false;
                }
            }

            return true;
        }
    }
}
