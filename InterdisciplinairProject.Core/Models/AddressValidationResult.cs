namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents the result of a DMX address validation check.
/// </summary>
public class AddressValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddressValidationResult"/> class.
    /// </summary>
    public AddressValidationResult()
    {
        Conflicts = new List<AddressConflict>();
        Warnings = new List<string>();
    }

    /// <summary>
    /// Gets a value indicating whether the validation passed without conflicts.
    /// </summary>
    public bool IsValid => Conflicts.Count == 0 && !ExceedsDmxUniverse;

    /// <summary>
    /// Gets or sets a value indicating whether the fixture exceeds the DMX universe limit (512).
    /// </summary>
    public bool ExceedsDmxUniverse { get; set; }

    /// <summary>
    /// Gets or sets the list of address conflicts found.
    /// </summary>
    public List<AddressConflict> Conflicts { get; set; }

    /// <summary>
    /// Gets or sets the list of warning messages (non-blocking issues).
    /// </summary>
    public List<string> Warnings { get; set; }

    /// <summary>
    /// Gets or sets the suggested next available start address (if current address has conflicts).
    /// </summary>
    public int? SuggestedStartAddress { get; set; }

    /// <summary>
    /// Gets a summary of all validation issues.
    /// </summary>
    public string Summary
    {
        get
        {
            if (IsValid)
            {
                return "Address validation passed.";
            }

            var issues = new List<string>();

            if (ExceedsDmxUniverse)
            {
                issues.Add("Fixture exceeds DMX universe limit (512 channels).");
            }

            foreach (var conflict in Conflicts)
            {
                issues.Add(conflict.Description);
            }

            return string.Join("\n", issues);
        }
    }
}
