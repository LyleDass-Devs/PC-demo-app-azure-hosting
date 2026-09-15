namespace KubaPC.Models.Internal
{
    // Money lives in the database as integer cents. These two helpers are the only place rands and cents meet.
    public static class Money
    {
        private const decimal CENTS_PER_RAND = 100m;

        public static long ToMinor(decimal rands) => (long)Math.Round(rands * CENTS_PER_RAND, 0, MidpointRounding.AwayFromZero);

        public static decimal ToRands(long minor) => minor / CENTS_PER_RAND;
    }
}
