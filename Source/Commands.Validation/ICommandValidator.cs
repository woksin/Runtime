/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
namespace Dolittle.Runtime.Commands.Validation
{
    /// <summary>
    /// Validates that a command is valid and conforms to simple business rules
    /// </summary>
    public interface ICommandValidator
    {
        /// <summary>
        /// Validate the command
        /// </summary>
        /// <param name="command">Instance to be validated</param>
        /// <returns>Validation results for a <see cref="CommandRequest">Command</see></returns>
        CommandValidationResult Validate(CommandRequest command);
    }
}