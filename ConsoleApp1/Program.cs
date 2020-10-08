using System;
using System.Linq;

namespace ConsoleApp1
{
    class Program
    {
        enum asd { q = 0, w, e, r};

        static void Main(string[] args)
        {
            asd a = asd.q;
            for (int i = 0; i < 10; ++i)
            {
                if (Enum.IsDefined(typeof(asd), a + 1))
                {
                    a += 1;
                }
                else
                {
                    a = Enum.GetValues(typeof(asd)).Cast<asd>().ElementAt(0);
                }

                Console.WriteLine(a);
            }
        }
    }
}
