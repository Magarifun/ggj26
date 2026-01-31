using System.Collections.Generic;

public class RomanNumeral
{
    private static Dictionary<int, string> mapping = new()
    {
                { 1000, "M" },
                { 900, "CM" },
                { 500, "D" },
                { 400, "CD" },
                { 100, "C" },
                { 90, "XC" },
                { 50, "L" },
                { 40, "XL" },
                { 10, "X" },
                { 9, "IX" },
                { 5, "V" },
                { 4, "IV" },
                { 1, "I" }
            };
    private int number;


    public RomanNumeral(int number)
    {
        this.number = number;
    }

    public override string ToString()
    {
        return ToRoman(number);
    }

    private static string ToRoman(int num)
    {
        foreach (KeyValuePair<int, string> kv in mapping)
        {
            if (num >= kv.Key)
                return kv.Value + ToRoman(num - kv.Key);
        }
        return "";
    }

}
