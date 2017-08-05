using System.Linq;
using System.Text.RegularExpressions;

namespace Cavokator
{
    class WxTaforDivider
    {

        /// <summary>
        /// Takes a tafor string and divides in PROBXX TEMPO, TEMPO and FM
        /// </summary>
        /// <param name="rawTafor"></param>
        /// <returns></returns>
        public string DivideTafor(string rawTafor)
        {

            string dividedTafor = rawTafor;


            // PROBXX TEMPO || TEMPO || FM (USA)
            var tempoRegex = new Regex(@"(PROB[0-9]{2} TEMPO)|(TEMPO)|(BECMG)|(FM)[0-9]{6}");
            var tempoMatches = tempoRegex.Matches(rawTafor);
            var tempoMatchNumber = 0;
            foreach (var match in tempoMatches.Cast<Match>())
            {

                try
                {
                    var firstPart = dividedTafor.Substring(0, match.Index + 1 * tempoMatchNumber);
                    var secondPart = dividedTafor.Substring(firstPart.Length, dividedTafor.Length - firstPart.Length);

                    dividedTafor = firstPart + "\n" + secondPart;

                    tempoMatchNumber++;

                }
                catch
                {
                    // Ignored
                }

            }


            return dividedTafor;
        }
    }
}