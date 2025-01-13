using System.Text;
using EMR.Application.Interfaces.Repositories;
using EMR.Application.Interfaces.Services;

namespace EMR.Application.Services;

public class NumService(IUnitOfWork<string> unitOfWork) : INumService
{
    private readonly IUnitOfWork<string> _unitOfWork = unitOfWork;

    public class Num(string character)
    {
        public string Type { get; set; } // Maybe you want to have different types of ticket?
        public string AlphaPrefix { get; set; } = character;
        public string NumericPrefix { get; set; } = "000001";
        public int No { get; set; }

        public void Increment(string character)
        {
            var num = int.Parse(NumericPrefix);

            if (num + 1 >= 999999)
            {
                num = No;

                var i = 2; // We are assuming that there are only 3 characters
                var isMax = AlphaPrefix == character;

                if (isMax)
                {
                    AlphaPrefix = character; // reset
                }
                else
                {
                    while (AlphaPrefix[i] == 'Z') i--;

                    var iChar = AlphaPrefix[i];

                    var sb = new StringBuilder(AlphaPrefix);

                    sb[i] = (char)(iChar + 1);

                    AlphaPrefix = sb.ToString();
                }
            }
            else
            {
                num++;
            }

            NumericPrefix = num.ToString().PadLeft(6, '0');
        }

        public override string ToString()
        {
            return AlphaPrefix + NumericPrefix;
        }
    }
}