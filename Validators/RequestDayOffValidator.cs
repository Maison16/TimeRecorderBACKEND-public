using FluentValidation;
using TimeRecorderBACKEND.Dtos;
using System;

public class RequestDayOffValidator : AbstractValidator<RequestDayOffModel>
{
    public RequestDayOffValidator()
    {
        RuleFor(x => x.DateStart)
            .NotEmpty().WithMessage("Start date is required.")
            .Must(date => date >= DateTime.Today).WithMessage("Start date cannot be in the past.");

        RuleFor(x => x.DateEnd)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.DateStart).WithMessage("End date must be after or equal to start date.")
            .Must(date => date >= DateTime.Today).WithMessage("End date cannot be in the past.");

        RuleFor(x => (x.DateEnd - x.DateStart).TotalDays)
            .LessThanOrEqualTo(30).WithMessage("Day off cannot be longer than 30 days.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.")
            .NotEmpty().When(x => (x.DateEnd - x.DateStart).TotalDays >= 5)
            .WithMessage("Reason is required for long leaves (5 days or more).");
    }
}