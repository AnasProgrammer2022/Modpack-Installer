using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// This provides various random tools used through the program.
/// </summary>
namespace Modpack_Installer.UserInteractive
{
    ///<summary>This class provieds functions for requesting the user's answer in different ways.</summary>
    class PromptUserAnswer
    {
        /// <summary>
        /// Requests an answer that expects agreement or rejection. Anything other than the two is rejected.
        /// </summary>
        /// <param name="message">The question to display it for the user</param>
        /// <returns>True if the user accepts. False if the user rejects.</returns>
        public static bool YorNAnswer(string message)
        {
            string userAnswer = string.Empty;
            Console.WriteLine(message);
            while (true)
            {
                userAnswer = Console.ReadLine();
                if (userAnswer.ToLower().Equals("yes") || userAnswer.ToLower().Equals("y")) return true;
                else if (userAnswer.ToLower().Equals("no") || userAnswer.ToLower().Equals("n")) return false;
                else
                {
                    Console.Clear();
                    Console.WriteLine("Answer by either yes, \'y\', no or \'n\'");
                    Console.WriteLine(message);
                }
            }
        }

        /// <summary>
        /// (yes or no or skip) Requests an answer that expects agreement, rejection or rejects when unvalid answer is entered.
        /// </summary>
        /// <param name="acceptExpectedAnswer">What is the expected answer for agreeing.</param>
        /// <param name="rejectExpectedAnswer">What is the expected answer for rejecting.</param>
        /// <returns>1 if the user accepted. 0 if the user rejected. -1 if none of them is entered.</returns>
        public static int YorNAnswerSkip(string acceptExpectedAnswer, string rejectExpectedAnswer)
        {
            string userAnswer = string.Empty;
            while (true)
            {
                userAnswer = Console.ReadLine();
                if (userAnswer.ToLower().Contains(acceptExpectedAnswer)) return 1;
                else if (userAnswer.ToLower().Contains(rejectExpectedAnswer)) return 0;
                else return -1;
            }
        }

        /// <summary>
        /// Shows a question and requests an answer from valid answers which are determined.
        /// </summary>
        /// <param name="message">The question to show the user for answer.</param>
        /// <param name="expectedAnswers">A string array of expected answers.</param>
        /// <param name="nonExpectedAnswerErrorMessage">What to show to the user when a non expected answer is entered.</param>
        /// <returns>An integer index that determines the string through the expected answers array.</returns>
        public static int ValidAnswers(string message, string[] expectedAnswers, string errMessage)
        {
            string userAnswer;
            bool isCorrect = true;
            while (true)
            {
                Console.Clear();
                if (!isCorrect) Console.WriteLine(errMessage);
                Console.WriteLine(message);
                userAnswer = Console.ReadLine();
                for (int i = 0; i < expectedAnswers.Length; i++)
                    if (string.Compare(userAnswer, expectedAnswers[i]) == 0)
                        return i;
                isCorrect = false;
            }
        }
        /// <summary>
        /// Shows a question and requests an answer from valid answers which are determined.
        /// </summary>
        /// <param name="message">The question to show the user for answer.</param>
        /// <param name="expectedAnswers">A string array of expected answers.</param>
        /// <param name="nonExpectedAnswerErrorMessage">What to show to the user when a non expected answer is entered.</param>
        /// <param name="errorMessageColor">The color of the error message, making it visibely special to the user.</param>
        /// <returns>An integer index that determines the string through the expected answers array.</returns>
        public static int ValidAnswers(string message, string[] expectedAnswers, string errMessage, ConsoleColor errorMessageColor)
        {
            string userAnswer;
            bool isCorrect = true;
            while (true)
            {
                Console.Clear();
                if (!isCorrect) Console.WriteLine(errMessage);
                Console.WriteLine(message);
                userAnswer = Console.ReadLine();
                for (int i = 0; i < expectedAnswers.Length; i++)
                    if (string.Compare(userAnswer, expectedAnswers[i]) == 0)
                        return i;
                isCorrect = false;
            }
        }

        public static int IndexAnswers(int minIndex, int maxIndex, string errMessage)
        {
            int userAnswer;
            while (true)
            {
                try
                {
                    userAnswer = Convert.ToInt32(Console.ReadLine());
                    if (userAnswer >= minIndex && userAnswer <= maxIndex)
                        return userAnswer;
                    else
                        Console.WriteLine(errMessage);
                }
                catch { Console.WriteLine(errMessage); }

            }
        }
    }

    public class Tools
    {
        /// <summary>
        /// It converts an RGB hex string (FFFFFF) to individual int RGB values
        /// </summary>
        /// <param name="hexValue">The hex string to convert from</param>
        /// <returns>Three int values, first for Red value, second for Green, and third for Blue.</returns>
        public static int[] HexStringToInt(string hexValue)
        {
            if (hexValue.Length < 6)
                hexValue = new string('0', 6 - hexValue.Length) + hexValue;
            string[] RGBstrings = { hexValue.Substring(0, 2), hexValue.Substring(2, 2), hexValue.Substring(4, 2) };
            int[] RGBints = { Convert.ToUInt16(RGBstrings[0], 16), Convert.ToUInt16(RGBstrings[1], 16), Convert.ToUInt16(RGBstrings[2], 16) };
            decimal hueVerify = RGBints[0] * 0.299m + RGBints[1] * 0.587m + RGBints[0] * 0.114m;
            if (hueVerify < 120)
            {
                decimal ratio = 120 / hueVerify;
                RGBints[0] = (int)(ratio * RGBints[0]);
                RGBints[1] = (int)(ratio * RGBints[1]);
                RGBints[2] = (int)(ratio * RGBints[2]);
            }
            return RGBints;
        }
    }
}
