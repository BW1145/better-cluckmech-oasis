using System;
using System.Collections.Generic;
using CluckmechCoop;
using Mirror;

static class Program
{
    private static int checks;
    private static void Check(bool value, string name)
    {
        checks++;
        if (!value) throw new Exception(name);
    }

    static void Main()
    {
        OtherworldProtocol.Register();
        // Captured v0.6.4 layout: fixed-width little-endian numbers, Mirror UTF-8 strings.
        const string sample = "040302010807060504000000020000000000803F000000C000006040000000BF0000A03F000020410400E7A59E03007B7D";
        var message = new OtherworldMessage
        {
            Player = 0x01020304, Sequence = 0x05060708, Kind = 4, Value = 2,
            X = 1, Y = -2, EndX = 3.5f, EndY = -.5f, Duration = 1.25f, Amount = 10,
            Token = "神", Data = "{}"
        };
        var writer = new NetworkWriter();
        Writer<OtherworldMessage>.write(writer, message);
        Check(Convert.ToHexString(writer.ToArray()) == sample, "v0.6.4 fixed byte sample");
        var reader = new NetworkReader(Convert.FromHexString(sample));
        var read = Reader<OtherworldMessage>.read(reader);
        Check(read.Player == message.Player && read.Sequence == message.Sequence && read.Kind == message.Kind && read.Value == message.Value, "identity and command fields round trip");
        Check(read.X == 1 && read.Y == -2 && read.EndX == 3.5f && read.EndY == -.5f && read.Duration == 1.25f && read.Amount == 10 && read.Token == "神" && read.Data == "{}", "all payload fields round trip");
        Check(reader.Remaining == 0, "fixed sample consumed exactly");
        writer.Reset();
        Writer<OtherworldMessage>.write(writer, new OtherworldMessage { Token = null, Data = "" });
        Check(Convert.ToHexString(writer.ToArray()) == new string('0', 80) + "00000100", "null and empty string fixed sample");
        var empty = Reader<OtherworldMessage>.read(new NetworkReader(writer.ToArray()));
        Check(empty.Token == null && empty.Data == "", "null differs from empty");
        Check(OtherworldMessageKind.Cast == 0 && OtherworldMessageKind.BeginPhase == 1 && OtherworldMessageKind.ReleaseInput == 2 && OtherworldMessageKind.ReservedCommand == 3 && OtherworldMessageKind.Dash == 4 &&
            OtherworldMessageKind.State == 10 && OtherworldMessageKind.Effect == 11 && OtherworldMessageKind.Beam == 12 && OtherworldMessageKind.Transfer == 14 && OtherworldMessageKind.AddEnergy == 15 && OtherworldMessageKind.EnsureRoom == 16 &&
            OtherworldMessageKind.Progress == 20 && OtherworldMessageKind.Drop == 21 && OtherworldMessageKind.SubmitProgress == 30 && OtherworldMessageKind.CollectDrop == 31, "published message numbers retained");
        Check(OtherworldMessageKind.IsCommand(0) && OtherworldMessageKind.IsCommand(4) && !OtherworldMessageKind.IsCommand(-1) && !OtherworldMessageKind.IsCommand(5) && !OtherworldMessageKind.IsCommand(30), "command range excludes server messages");
        var run = new OtherworldRun { Hero = 12, Initial = 26001, Talents = new HashSet<int> { 26104, 26105 }, BaseInfluence = -.1f };
        Check(OtherworldDataValidation.Run(run) && OtherworldDataValidation.NetworkRun(run), "valid player data accepted at both boundaries");
        run.Initial = 0;
        Check(OtherworldDataValidation.Run(run) && !OtherworldDataValidation.NetworkRun(run), "legacy missing initial accepted only on disk");
        run.Initial = 26002;
        Check(OtherworldDataValidation.Run(run) && !OtherworldDataValidation.NetworkRun(run), "legacy branch accepted on disk but not wire");
        run.Initial = 26001;
        run.Talents = new HashSet<int> { 26101, 26102, 26103, 26104, 26105, 26106, 26107 };
        Check(OtherworldDataValidation.Run(run) && !OtherworldDataValidation.NetworkRun(run), "network talent count cap retained separately");
        run.Talents = new HashSet<int> { 26113 };
        Check(!OtherworldDataValidation.Run(run), "foreign hero talent rejected by shared rule");
        run.Talents = new HashSet<int> { 26104 };
        run.BaseInfluence = float.NaN;
        Check(!OtherworldDataValidation.Run(run) && !OtherworldDataValidation.NetworkRun(run), "nonfinite player modifier rejected at both boundaries");
        run.BaseInfluence = 0;
        run.LastDeployment = new Machine { Id = 1, Upgrades = null };
        Check(!OtherworldDataValidation.Run(run), "malformed machine snapshot rejected at both boundaries");
        run.LastDeployment = null;
        run.CardIds.Add("same"); run.CardIds.Add("same");
        Check(!OtherworldDataValidation.Run(run), "duplicate persistent identity rejected");
        Console.WriteLine($"Otherworld protocol: {checks} checks passed using actual Mirror assembly.");
    }
}
