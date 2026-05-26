namespace CloudNativeApp.Services;

public class TaxService
{
    private const decimal TaxRate = 0.1m;

    public decimal Calculate(decimal subTotal)
        => Math.Floor(subTotal * TaxRate);
}
