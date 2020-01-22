using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageGaussianBlur
{
    class ImagePreparer
    {
        public List<float> getListOfSurroundingPixelsRgbValues(Bitmap givenImage, int position)
        {
            List<float> lisfOfRGBValues = new List<float>(); // list of RGB values to be return

            int currentPosition = position + 1;
            int imageWidth = givenImage.Width - 2; // ignoring border pixels
            int calculatedXPosition;
            int calculatedYPosition = 1;

            while (currentPosition > imageWidth)
            {
                calculatedYPosition++;
                currentPosition -= imageWidth;
            }
            calculatedXPosition = currentPosition;

            // adding 9 pixels (RGB Values of pixels that surround pixel at given position) to the list
            for (int i = calculatedXPosition - 1; i <= calculatedXPosition + 1; i++)
            {
                for (int j = calculatedYPosition - 1; j <= calculatedYPosition + 1; j++)
                {
                    Color pixelToAdd = givenImage.GetPixel(i, j);
                    lisfOfRGBValues.Add(pixelToAdd.R); // Adding red part of the pixel
                    lisfOfRGBValues.Add(pixelToAdd.G); // Adding green part of the pixel
                    lisfOfRGBValues.Add(pixelToAdd.B); // Adding blue parto of the pixel
                    // ignoring useless Alpha channel
                }
            }

            return lisfOfRGBValues;
        }

        public Bitmap transformArrayToImage(Bitmap currentImage, float[][] arrayOfRgb)
        {
            int currentIndex = 0;
            // set new values for all pixels
            for (int i = 1; i < currentImage.Height - 1; i++)
            {
                for (int j = 1; j < currentImage.Width - 1; j++)
                {
                    int newRed = (int)arrayOfRgb[currentIndex][0];
                    int newGreen = (int)arrayOfRgb[currentIndex][1];
                    int newBlue = (int)arrayOfRgb[currentIndex][2];

                    // checking if values are not to big
                    if (newRed > 255)
                    {
                        newRed = 255;
                    }
                    if (newGreen > 255)
                    {
                        newGreen = 255;
                    }
                    if (newBlue > 255)
                    {
                        newBlue = 255;
                    }

                    currentImage.SetPixel(j, i, Color.FromArgb(newRed, newGreen, newBlue));
                    currentIndex++;
                }
            }

            return currentImage;
        }


    }
}
