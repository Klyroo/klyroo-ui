namespace Dashboard.Shared.Data;

/// <summary>A message of the inbox.</summary>
public sealed record Mail(int Id, string From, string Email, string Subject, string[] Body, DateTimeOffset Date, string Label, bool Unread);

/// <summary>The sample messages of the Inbox page, newest first.</summary>
public static class MailData
{
    public static readonly Mail[] All = Build();

    private static Mail[] Build()
    {
        var now = new DateTimeOffset(DemoClock.Today.ToDateTime(new TimeOnly(9, 30)), TimeSpan.Zero);
        Mail Make(int id, string from, string email, string subject, TimeSpan ago, string label, bool unread, params string[] body) =>
            new(id, from, email, subject, body, now - ago, label, unread);

        return
        [
            Make(1, "Ingrid Holmgren", "ingrid@copperleaf.example", "Renewal for the Business plan", TimeSpan.FromMinutes(18), "Billing", true,
                "Hi, our contract with you ends on the 30th and we would like to renew the Business plan for another year.",
                "Could you send a quote that includes the extra seats we discussed? Eight more would cover the new team.",
                "Thanks, Ingrid"),
            Make(2, "Tomas Brandvold", "tomas@ironbark.example", "Question about exporting orders", TimeSpan.FromHours(2), "Support", true,
                "Is there a way to export the orders of a single customer as a spreadsheet? The table lets me filter, but I cannot find an export button.",
                "I need it for an audit next week, so even a rough answer today would help."),
            Make(3, "Celeste Marchetti", "celeste@foxglove.example", "Feedback on the new reports", TimeSpan.FromHours(5), "Product", false,
                "We have been using the reports for two weeks now and the team likes the revenue view. Two small things: the period selector forgets its value when we reload, and the donut could show percentages.",
                "No rush, but we wanted you to hear it from us first."),
            Make(4, "Wiktor Novak", "wiktor@juniperrow.example", "Invoice 5139 is still open", TimeSpan.FromHours(9), "Billing", true,
                "Our accounting software shows invoice 5139 as unpaid, but we sent the transfer on the 3rd. I attached the receipt to the invoice in your portal.",
                "Can you confirm that it arrived?"),
            Make(5, "Sunniva Eklund", "sunniva@larkspur.example", "Welcome call on Thursday", TimeSpan.FromDays(1), "Onboarding", false,
                "Thank you for booking the welcome call. We will walk through the import of your customers and set up the first team members.",
                "Please have your list of users ready. A CSV with a name and an email per line is enough."),
            Make(6, "Rafael Dumont", "rafael@tidewater.example", "Can we add a second owner?", TimeSpan.FromDays(1) + TimeSpan.FromHours(3), "Support", false,
                "Our founder travels a lot and we would like a second person to be able to manage the plan and the members. Is that possible without sharing a login?"),
            Make(7, "Katya Petrov", "katya@nimbus.example", "Partnership idea", TimeSpan.FromDays(2), "Partners", false,
                "I run a small studio that builds internal tools for logistics companies. Several of our clients ask for exactly your kind of product.",
                "Would you be open to a call about a referral program?"),
            Make(8, "Hugo Lindqvist", "hugo@mossgate.example", "Security questionnaire", TimeSpan.FromDays(2) + TimeSpan.FromHours(6), "Security", false,
                "Our procurement team needs the answers to the attached security questionnaire before they can sign. It covers hosting, backups and access control.",
                "The deadline is the end of the month."),
            Make(9, "Greta Fontaine", "greta@quillfeather.example", "Thanks for the quick fix", TimeSpan.FromDays(3), "Support", false,
                "The sign-in problem is gone since this morning. Thank you for answering so fast on a Sunday."),
            Make(10, "Dario Castellan", "dario@halcyon.example", "Trial extension", TimeSpan.FromDays(4), "Sales", false,
                "We loved the trial but the budget is only released next month. Could you extend it by two weeks?",
                "We are already running two of our projects on it."),
            Make(11, "Aiko Tanaka", "aiko@driftwood.example", "Workshop notes", TimeSpan.FromDays(5), "Product", false,
                "Here are the notes from yesterday's workshop. The three themes that came up most were faster onboarding, better filters in tables and a mobile view of the inbox."),
            Make(12, "Fabian Whitlock", "fabian@brightloom.example", "Change of billing address", TimeSpan.FromDays(6), "Billing", false,
                "We moved offices at the start of the month. Please update the billing address on our account and on the next invoice."),
        ];
    }
}
