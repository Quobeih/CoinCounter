using OpenCvSharp; // For OpenCVSharp
using OpenCvSharp.Extensions; // To convert between OpenCV Mat and Bitmap
using System;
using System.Windows.Forms;

namespace CoinCounter
{
    public partial class Form1 : Form
    {
        private Mat uploadedImage; // To store the uploaded image

        public Form1()
        {
            InitializeComponent();
        }

        // Upload Image Button Click
        private void uploadImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                openFileDialog.Title = "Select an Image";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Load the image into OpenCV Mat
                    uploadedImage = Cv2.ImRead(openFileDialog.FileName);

                    // Display the image in the PictureBox
                    pictureBox.Image = BitmapConverter.ToBitmap(uploadedImage);
                    pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                }
            }
        }

        // Process Image to Count Coins
        private void processImage_Click(object sender, EventArgs e)
        {
            if (uploadedImage == null)
            {
                MessageBox.Show("Please upload an image first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Step 1: Preprocess the Image (Grayscale and Gaussian Blur)
            Mat gray = new Mat();
            Cv2.CvtColor(uploadedImage, gray, ColorConversionCodes.BGR2GRAY);

            Mat blurred = new Mat();
            Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(15, 15), 0);

            // Step 2: Detect Circles Using Hough Circle Transform
            CircleSegment[] circles = Cv2.HoughCircles(
                blurred,
                HoughModes.Gradient,
                dp: 1.2,             // Resolution ratio
                minDist: 30,         // Minimum distance between detected circles
                param1: 200,         // High threshold for Canny edge detection
                param2: 30,          // Threshold for center detection
                minRadius: 5,        // Minimum radius of detected circles
                maxRadius: 50        // Maximum radius of detected circles
            );

            if (circles.Length == 0)
            {
                MessageBox.Show("No coins detected in the image.", "No Coins", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Step 3: Coin Radius-to-Value Mapping
            var coinValues = new[]
            {
               // new { Radius = 10, Value = 0.05 }, // Example: Coin with ~10px radius is worth $0.05
                 new { Radius = 25, Value = 0.05 },
                 new { Radius = 30, Value = 0.10 },
                 new { Radius = 35, Value = 0.25 },
                 new { Radius = 42, Value = 1.00 },
                 new { Radius = 50, Value = 5.00 }
            };

            double totalValue = 0.0;

            // Step 4: Iterate Over Detected Circles
            foreach (var circle in circles)
            {
                var center = new OpenCvSharp.Point((int)circle.Center.X, (int)circle.Center.Y);
                int radius = (int)circle.Radius;

                // Match Radius to Coin Value
                double? coinValue = null;
                foreach (var coin in coinValues)
                {
                    if (Math.Abs(radius - coin.Radius) <= 5) // Allow a margin of ±5 pixels
                    {
                        coinValue = coin.Value;
                        break;
                    }
                }

                if (coinValue.HasValue)
                {
                    totalValue += coinValue.Value;

                    // Annotate Detected Coin
                    AnnotateCoin(uploadedImage, center, radius, coinValue.Value);
                }
            }

            // Step 5: Display Results
            pictureBox.Image = BitmapConverter.ToBitmap(uploadedImage);
            MessageBox.Show($"Total Value of Coins: P{totalValue:F2}", "Total Value", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Helper Method: Annotate Coins on the Image
        private void AnnotateCoin(Mat image, OpenCvSharp.Point center, int radius, double value)
        {
            // Draw a green circle around the coin
            Cv2.Circle(image, center, radius, Scalar.Green, 5);

            // Add the coin value as a label
            Cv2.PutText(
                image,
                $"P{value:F2}",
                new OpenCvSharp.Point(center.X - 10, center.Y - 10),
                HersheyFonts.HersheySimplex,
                1.0,
                Scalar.Red,
                2
            );
        }
    }
}
