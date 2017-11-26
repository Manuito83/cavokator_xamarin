namespace Cavokator
{
    public class WxRwyCondition
    {
        // ERROR
        public bool MainError;

        public bool SNOCLO;

        public bool CLRD;
        
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
        public bool DepositError;

        // **** EXTENT BLOCK ****
        /// <summary>
        /// Extent code (1,2,5,9) or /
        /// </summary>
        public string ExtentCode;
        public bool ExtentError;

        // **** DEPTH BLOCK ****
        /// <summary>
        /// Depth code (00 -> 90, 92 -> 99) or //
        /// </summary>
        public string DepthCode;
        public int DepthValue;
        public bool DepthError;

        // **** FRICTION BLOCK ****
        /// <summary>
        /// Friction code (00 -> 95, 99) or //
        /// </summary>
        public string FrictionCode;
        public int FrictionValue;
        public bool FrictionError;
   
    }
}