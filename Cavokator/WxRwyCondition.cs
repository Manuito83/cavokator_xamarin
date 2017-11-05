namespace Cavokator
{
    public class WxRwyCondition
    {
        // ERROR
        public bool MainError;
        
        // **** RUNWAY BLOCK ****
        /// <summary>
        /// Actual string (e.g.: "R14L")
        /// </summary>
        public string RwyCode;
        
        /// <summary>
        /// Runway designator (e.g.: "14L")
        /// </summary>
        public string RwyValue;

        /// <summary>
        /// Runway number integet (e.g.:"14")
        /// </summary>
        public int RwyInt;

        /// <summary>
        /// Runway error
        /// </summary>
        public bool RwyError;

        // **** DEPOSITS BLOCK ****
        /// <summary>
        /// Deposit code (0-9) or /
        /// </summary>
        public string DepositCode;

        /// <summary>
        /// Deposit error
        /// </summary>
        public bool DepositError;

        // EXTENT OF CONTAMINATION
        public string ExtentCode;
        public string ExtentValue;
        public bool ExtentError;

        // DEPTH OF DEPOSITS
        public string DepthCode;
        public string DepthValue;
        public bool DepthError;


        // FRICTION COEFFICIENT
        public string FrictionCode;
        public string FrictionValue;
        public bool FrictionError;
   
    }
}