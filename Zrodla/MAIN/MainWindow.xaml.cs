using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Win32;
using System.Management;

namespace ImageGaussianBlur
{
    public partial class MainWindow : Window
    {
        //  import C++ DLL
        [DllImport("DLL_C.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void filterImage(IntPtr filterPointer, IntPtr arrayPointer, int length, float filterNorm);

        // import asm DLL
        [DllImport("DLL_ASM.dll")]
        public static extern unsafe void filterProc(IntPtr filterPointer, IntPtr arrayPointer, int length, float filterNorm);

        private string originalImagePath; // path to original image
        private bool isASM;

        private Bitmap imageToFilter;
        private float[][] arrayOfRgbValuesOfPixelAndItsSurrounding;
        private ImagePreparer imagePreparer = new ImagePreparer();

        public MainWindow()
        {
            int numberOfCores = 0;
            // zliczenie liczby dostepnych rdzeni
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_Processor").Get()) // zliczenie liczby dostepnych rdzeni
            {
                numberOfCores += int.Parse(item["NumberOfCores"].ToString());
            }

            InitializeComponent();

            recommendedThreadsLabel.Text = numberOfCores.ToString();
            ThreadsSlider.Value = numberOfCores;
        }
        private void OnLoadImageButtonClicked(object sender, RoutedEventArgs e)
        {
            var newFileDialog = new OpenFileDialog();
            newFileDialog.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png";

            if (newFileDialog.ShowDialog() == true)
            {
                originalImagePath = newFileDialog.FileName;
                ImageSource originalImageSource = new BitmapImage(new Uri(newFileDialog.FileName));
                OriginalImage.Source = originalImageSource;
            }

            try
            {
                imageToFilter = new Bitmap(originalImagePath); // loading image to filter from file
                int lengthOfPointerArray = (imageToFilter.Height - 2) * (imageToFilter.Width - 2); // length of pointer array ( number of all pixels to be processed ), ignoring borders

                // array that contains information about rgb values of all pixels of the image, that will be processed,
                // together with information of rbg values of pixels that surround each of these pixels
                arrayOfRgbValuesOfPixelAndItsSurrounding = new float[lengthOfPointerArray][];

                // filling created array with values from loaded image
                for (int i = 0; i < lengthOfPointerArray; i++)
                {
                    arrayOfRgbValuesOfPixelAndItsSurrounding[i] = imagePreparer.getListOfSurroundingPixelsRgbValues(imageToFilter, i).ToArray();
                }
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show("No file was choosen");
            }
        }

        private void OnConvertButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                int lengthOfPointerArray = arrayOfRgbValuesOfPixelAndItsSurrounding.Length;
                // array of pointer to rgb array created in order to allocate memory and copy values
                IntPtr[] rgbArrayPointerArray = new IntPtr[lengthOfPointerArray];

                for (int i = 0; i < lengthOfPointerArray; i++)
                {
                    rgbArrayPointerArray[i] = Marshal.AllocHGlobal(sizeof(float) * arrayOfRgbValuesOfPixelAndItsSurrounding[i].Length); // alocating memory in rgbArrayPointerArray
                                                                                                                                        // copying values from arrayOfRgbValuesOfPixelAndItsSurrounding to rgbArrayPointerArray
                    Marshal.Copy(arrayOfRgbValuesOfPixelAndItsSurrounding[i], 0, rgbArrayPointerArray[i], arrayOfRgbValuesOfPixelAndItsSurrounding[i].Length);
                }

                /// do tego trzeba jeszcze zwolnić pamięć
                float[] gaussFilter = { 1, 2, 1, 2, 4, 2, 1, 2, 1 }; // float array with filter
                IntPtr filterPointer = new IntPtr(); // pointer to alocate memory and copy values
                filterPointer = Marshal.AllocHGlobal(sizeof(float) * 9); // alocating memory 
                Marshal.Copy(gaussFilter, 0, filterPointer, 9); // copying data to alocated memory

                string executionTime;
                // calling functions in multiple threads
                if (isASM)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew(); // time measurement
                    Parallel.For(0, rgbArrayPointerArray.Length, new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(ThreadsSlider.Value) }, i =>
                    {
                        filterProc(filterPointer, rgbArrayPointerArray[i], arrayOfRgbValuesOfPixelAndItsSurrounding[i].Length, 16.0f);
                    });
                    stopwatch.Stop();
                    executionTime = stopwatch.ElapsedMilliseconds.ToString() + "ms";
                    stopwatch.Reset();
                }
                else // CPP
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    Parallel.For(0, rgbArrayPointerArray.Length, new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(ThreadsSlider.Value) }, i =>
                    {
                        filterImage(filterPointer, rgbArrayPointerArray[i], arrayOfRgbValuesOfPixelAndItsSurrounding[i].Length, 16.0f);
                    });
                    stopwatch.Stop();
                    executionTime = stopwatch.ElapsedMilliseconds.ToString() + "ms";
                    stopwatch.Reset();
                }

                // powrotne kopiowanie wartosci do tablicy i zwolnienie pamieci
                Timer.Text = executionTime; // display processing time
                for (int i = 0; i < lengthOfPointerArray; i++)
                {
                    // copying prodessed data back to original array ( only 3 first elements of each array, the rest is unchanged
                    Marshal.Copy(rgbArrayPointerArray[i], arrayOfRgbValuesOfPixelAndItsSurrounding[i], 0, 3);
                    Marshal.FreeHGlobal(rgbArrayPointerArray[i]); // freeing alocated memory
                }

                // freeing memory of filter
                Marshal.FreeHGlobal(filterPointer);

                // setting new values for each pixel in image
                imageToFilter = imagePreparer.transformArrayToImage(imageToFilter, arrayOfRgbValuesOfPixelAndItsSurrounding);

                // Saving new image to file and creating new name for a file
                string usedThreads = Convert.ToString(Convert.ToInt32(ThreadsSlider.Value));
                string outputDirectoryPath = originalImagePath;
                string fileName = Path.GetFileNameWithoutExtension(outputDirectoryPath);
                string CppOrAsm;
                if (isASM)
                {
                    CppOrAsm = "ASM";
                }
                else
                {
                    CppOrAsm = "C";
                }
                string newFileName = fileName + " [" + CppOrAsm + "] threads [" + usedThreads + "])";
                string newOutputDirectioryPath = outputDirectoryPath.Replace(fileName, newFileName);

                imageToFilter.Save(newOutputDirectioryPath);
                ImageSource convertedImageSource = new BitmapImage(new Uri(newOutputDirectioryPath));
                ConvertedImage.Source = convertedImageSource;
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("You must choose image first");
            }
            catch (ExternalException)
            {
                MessageBox.Show("There is another file with this name");
            }
        }

        private void ASMRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            isASM = true;
        }

        private void CRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            isASM = false;
        }
    }
}
