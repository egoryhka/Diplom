namespace Diplom.FuncModule
{
    public class Functions
    {
        public readonly GPU GPU;
        public readonly CPU CPU;
        public readonly BitmapFunc BitmapFunc;

        public Functions()
        {
            GPU = new GPU();
            CPU = new CPU();
            BitmapFunc = new BitmapFunc();
        }
    }
}