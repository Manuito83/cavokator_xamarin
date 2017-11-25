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

        // **** EXTENT BLOCK ****
        /// <summary>
        /// Extent code (1,2,5,9) or /
        /// </summary>
        public string ExtentCode;

        /// <summary>
        /// Extent error
        /// </summary>
        public bool ExtentError;

        // **** DEPTH BLOCK ****
        /// <summary>
        /// Depth code (00 -> 90, 91 -> 99) or /
        /// </summary>
        public string DepthCode;
        public bool DepthError;

        // **** FRICTION BLOCK ****
        public string FrictionCode;
        public bool FrictionError;
   
    }
}