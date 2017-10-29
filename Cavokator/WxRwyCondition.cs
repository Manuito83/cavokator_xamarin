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
        /// Runway number
        /// </summary>
        public string RwyValue;

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