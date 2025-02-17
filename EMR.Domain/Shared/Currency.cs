using System;
using System.Collections.Generic;

namespace EMR.Domain.Shared;

public sealed record Currency
{
    public string Code { get; }
    public string Symbol { get; }

    private static readonly Dictionary<string, Currency> _currencies = new();

    private Currency(string code, string symbol)
    {
        Code = code;
        Symbol = symbol;
    }

    public static Currency Get(string code)
    {
        code = code.ToUpperInvariant();
        if (!_currencies.ContainsKey(code))
        {
            _currencies[code] = new Currency(code, GetSymbolForCode(code));
        }
        return _currencies[code];
    }

    private static string GetSymbolForCode(string code) => code switch
    {
        "USD" => "$",
        "EUR" => "€",
        "KHR" => "៛",
        _ => code
    };

    public static Currency USD => Get("USD");
    public static Currency EUR => Get("EUR");
    public static Currency KHR => Get("KHR");
}