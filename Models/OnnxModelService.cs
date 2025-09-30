using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DodBreedClassifier.Models
{
	public class OnnxModelService
	{
		private readonly InferenceSession _session;
		private readonly string[] _classes;

		public OnnxModelService(IWebHostEnvironment env)
		{
			var modelPath = Path.Combine(env.ContentRootPath, "Models", "dogs_m.onnx");
			_session = new InferenceSession(modelPath);

			var classFile = Path.Combine(env.ContentRootPath, "Models", "class_names.txt");
			if (File.Exists(classFile))
				_classes = File.ReadAllLines(classFile);
			else
				_classes = Array.Empty<string>();
		}

		public string Predict(byte[] imageBytes)
		{
			using var image = Image.Load<Rgb24>(imageBytes);


			var inputMeta = _session.InputMetadata.First();
			string inputName = inputMeta.Key; 
			var dims = inputMeta.Value.Dimensions; // [-1,224,224,3]

			int height = dims[1];
			int width = dims[2];
			int channels = dims[3];

			image.Mutate(x => x.Resize(new ResizeOptions
			{
				Size = new Size(width, height),
				Mode = ResizeMode.Crop
			}));

			var inputTensor = new DenseTensor<float>(new[] { 1, height, width, channels });

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					var pixel = image[x, y];
					inputTensor[0, y, x, 0] = pixel.R / 255f;
					inputTensor[0, y, x, 1] = pixel.G / 255f;
					inputTensor[0, y, x, 2] = pixel.B / 255f;
				}
			}

			var inputs = new List<NamedOnnxValue>
			{
				NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
			};

			using var results = _session.Run(inputs);
			var output = results.First().AsEnumerable<float>().ToArray();

			int predictedIndex = Array.IndexOf(output, output.Max());

			if (_classes.Length > predictedIndex && predictedIndex >= 0)
				return _classes[predictedIndex];
			else
				return $"Unknown class (index {predictedIndex})";
		}
	}
}
