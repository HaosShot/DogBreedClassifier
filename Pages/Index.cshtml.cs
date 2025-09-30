using DodBreedClassifier.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DodBreedClassifier.Pages
{
    public class IndexModel : PageModel
    {
        private readonly OnnxModelService _modelService;
        private readonly IWebHostEnvironment _environment;

        public IndexModel(OnnxModelService modelService, IWebHostEnvironment environment)
        {
            _modelService = modelService;
            _environment = environment;
        }

        [BindProperty]
        public IFormFile File { get; set; }

        public string Prediction { get; set; }
        public string ImageUrl { get; set; }
        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (File == null || File.Length == 0)
            {
                ErrorMessage = "Пожалуйста, выберите файл";
                return Page();
            }

            try
            {
                if (!File.ContentType.StartsWith("image/"))
                {
                    ErrorMessage = "Пожалуйста, загрузите файл изображения";
                    return Page();
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(File.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await File.CopyToAsync(stream);
                }

                ImageUrl = $"/uploads/{fileName}";

                using var memoryStream = new MemoryStream();
                await File.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                Prediction = _modelService.Predict(imageBytes);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Произошла ошибка: {ex.Message}";
            }

            return Page();
        }
    }
}