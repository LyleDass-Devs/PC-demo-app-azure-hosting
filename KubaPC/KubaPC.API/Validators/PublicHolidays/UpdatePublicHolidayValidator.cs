using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.PublicHolidays.Requests;

namespace KubaPC.API.Validators.PublicHolidays
{
    public class UpdatePublicHolidayValidator : Validator<UpdatePublicHolidayRequest>
    {
        private const int EARLIEST_YEAR = 1990;

        public UpdatePublicHolidayValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.HolidayDate).NotEqual(default(DateOnly)).WithMessage("Choose the date.");
            RuleFor(x => x.HolidayDate.Year).InclusiveBetween(EARLIEST_YEAR, DateTime.UtcNow.Year + 5)
                .WithName("holidayDate").WithMessage("That year is outside the range the calendar holds.");
            RuleFor(x => x.HolidayName).NotEmpty().WithMessage("Enter the holiday name.").MaximumLength(100);
        }
    }
}
