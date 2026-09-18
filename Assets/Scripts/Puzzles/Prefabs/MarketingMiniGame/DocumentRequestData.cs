// Общий источник данных для DocumentApprovalGame (копирайтер) и справочника босса —
// один и тот же список, чтобы подсказка боссу никогда не разошлась с реальной игрой.
public static class DocumentRequestData
{
    public static readonly (string request, bool shouldApprove)[] All =
    {
        ("Buy 500 gold pens for HR", false),
        ("Paint the office to match the CEO's mood", false),
        ("Buy a 3D printer for printing cookies", false),
        ("Rent a helicopter for coffee deliveries", false),
        ("Buy 200 inflatable unicorns for the hallway", false),
        ("Hire a personal massage therapist for the stapler", false),
        ("Fund monthly company parties in Dubai", false),
        ("Buy 1,000 'Budget Blend' tea bags", true),
        ("Buy a new printer (the old one is on fire)", true),
        ("Repair the coffee machine (we are dying without coffee)", true),
        ("Buy A4 paper for printing documents", true),
        ("Buy chairs (employees are sitting on boxes)", true),
        ("Replace light bulbs (three weeks of working in the dark)", true),
        ("Buy toilet paper (critically important)", true),
        ("Buy a Wi-Fi router (our internet runs on pigeons)", true),
        ("Fund a course on 'How to Avoid Burnout'", false),
        ("Hire a personal barista for every employee", false),
        ("Buy a gold toilet for the VIP bathroom", false),
        ("Rent a yacht for 'team building'", false),
        ("Buy 50 hammocks for the open-plan office", false),
    };
}
