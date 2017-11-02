namespace Cavokator
{
    public class WxRwyCondition
    {
        // ERROR
        public bool MainError;
        
        // **** RUNWAY BLOCK ****
        /// <summary>
        /// Actual code from entered string
        /// </summary>
        public string RwyCode;
        
        /// <summary>
        /// Runway designator
        /// </summary>
        public string RwyValue;

        /// <summary>
        /// Runway number
        /// </summary>
        public int RwyInt;

        /// <summary>
        /// Runway error
        /// </summary>
        public bool RwyError;

        // RUNWAY DEPOSITS
        public string DepositCode;
        public string DepositValue;
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