﻿using System.Dynamic;
using Dolittle.Artifacts;
using Dolittle.Runtime.Transactions;
using Machine.Specifications;
using Moq;
using It = Machine.Specifications.It;

namespace Dolittle.Runtime.Commands.Coordination.Specs.for_CommandContextManager
{
    [Subject(Subjects.establishing_context)]
    public class when_establishing_with_different_commands : given.a_command_context_manager
    {
        static ICommandContext firstCommandContext;
        static ICommandContext secondCommandContext;

        Because of = () =>
                         {
                             var first_artifact = Artifact.New();
                             var second_artifact = Artifact.New();
                             var firstCommand = new CommandRequest(TransactionCorrelationId.NotSet, first_artifact.Id, first_artifact.Generation, new ExpandoObject());
                             var secondCommand = new CommandRequest(TransactionCorrelationId.NotSet, second_artifact.Id, second_artifact.Generation, new ExpandoObject());
                             firstCommandContext = Manager.EstablishForCommand(firstCommand);
                             secondCommandContext = Manager.EstablishForCommand(secondCommand);
                         };
 

        It should_return_different_contexts = () => firstCommandContext.ShouldNotEqual(secondCommandContext);
    }
}