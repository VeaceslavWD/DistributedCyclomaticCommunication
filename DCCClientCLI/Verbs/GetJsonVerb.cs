﻿namespace DCCClientCLI.Verbs
{
    using CommandLine;

    [Verb("get-json", HelpText = "Retrieves json from ...")]
    class GetJsonVerb : GetVerb
    {
        public GetJsonVerb() => VerbType = VerbType.Json;
    }
}