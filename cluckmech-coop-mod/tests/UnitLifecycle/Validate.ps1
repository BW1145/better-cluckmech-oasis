param([switch]$RuntimeOnly,[switch]$NotificationsOnly)
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../../dependencies/BepInEx/BepInEx/core/Mono.Cecil.dll')
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot '../../src/bin/Release/net472/CluckmechCoop.dll'))
function Get-ModType([string]$name){$assembly.MainModule.Types | Where-Object Name -EQ $name}
function Calls($method,[string]$name){@($method.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq $name})}
try {
    if($NotificationsOnly){
        $begin=(Get-ModType 'NetworkService').Methods | Where-Object Name -EQ 'BeginHandshake'
        if(-not (Calls $begin 'Register') -or (Calls $begin 'get_Enabled')){throw 'Session registration must not depend on local module preferences'}
        $register=(Get-ModType 'OtherworldNetwork').Methods | Where-Object Name -EQ 'Register'
        if(-not (Calls $register 'RegisterMessages')){throw 'Destroyed notifications must be registered before room creation'}
        $clear=(Get-ModType 'KamuiVisibility').Methods | Where-Object Name -EQ 'Clear'
        if(Calls $clear 'UnregisterHandler'){throw 'Scene clear removes a session-owned handler'}
        $disconnect=(Get-ModType 'OtherworldNetwork').Methods | Where-Object Name -EQ 'Disconnect'
        if(-not (Calls $disconnect 'Disconnect')){throw 'Session disconnect does not release notification registration'}
        'PASS: compiled unconditional session registration and scene/session notification ownership'
        return
    }
    if($RuntimeOnly){
        foreach($entry in @(@('Plugin','CaptureScene'),@('NetworkService','Handshake'),@('KamuiWorld','RestoreRoutine'),@('StratagemUnit','Start'),@('StratagemEffects','WaitForUnit'))){
            $iterator=(Get-ModType $entry[0]).NestedTypes | Where-Object Name -Like "<$($entry[1])>*"
            $move=$iterator.Methods | Where-Object Name -EQ 'MoveNext'
            if(-not (Calls $move 'IsCurrent')){throw "Delayed callback lacks scene guard: $($entry[0]).$($entry[1])"}
        }
        $combat=(Get-ModType 'Plugin').Methods | Where-Object Name -EQ 'CombatStarted'
        if(-not (Calls $combat 'BeginCombat')){throw 'Combat initialization is not scoped/idempotent'}
        $disconnect=(Get-ModType 'OtherworldNetwork').Methods | Where-Object Name -EQ 'Disconnect'
        if(@(Calls $disconnect 'UnregisterHandler').Count -ne 2){throw 'Otherworld handlers outlive their owning session'}
        $leave=(Get-ModType 'SceneLifecycle').Methods | Where-Object Name -EQ 'Leave'
        $close=@(Calls $leave 'Close')[0]
        $cleanup=@(Calls $leave 'ClearScene')[0]
        if(-not $close -or -not $cleanup -or $close.Offset -ge $cleanup.Offset){throw 'Scene cleanup precedes callback invalidation'}
        'PASS: compiled scene/session guards, repeated Begin gate and paired handler release'
        return
    }
    $transfer=(Get-ModType 'KamuiWorld').Methods | Where-Object Name -EQ 'Transfer'
    $invalidate=@(Calls $transfer 'ChangingSpace')[0]
    $position=@(Calls $transfer 'set_position')[0]
    if(-not $invalidate -or -not $position -or $invalidate.Offset -ge $position.Offset){throw 'Transfer must invalidate actions before moving the unit'}
    if(-not (Calls $transfer 'MovePosition')){throw 'Transfer must replace any pending Rigidbody pull target'}
    foreach($entry in @(@('OtherworldActions','Pull'),@('OtherworldActions','DelayedBombard'),@('OtherworldActions','OrbitalImpact'),@('OtherworldActions','Flame'),@('KamuiWorld','DashRoutine'))){
        $iterator=(Get-ModType $entry[0]).NestedTypes | Where-Object Name -Like "<$($entry[1])>*"
        $move=$iterator.Methods | Where-Object Name -EQ 'MoveNext'
        if(-not (Calls $move 'Capture') -or -not (Calls $move 'Current')){throw "Missing action lease: $($entry[1])"}
        if($entry[1] -eq 'Pull'){
            if(@(Calls $move 'Current')[0].Offset -ge @(Calls $move 'MovePosition')[0].Offset){throw 'Pull writes position before checking cancellation'}
        }
    }
    $removed=(Get-ModType 'UnitLifecycle').Methods | Where-Object Name -EQ 'Removed'
    foreach($name in @('Invalidate','Remove','Removed')){if(-not (Calls $removed $name)){throw "Lifecycle removal missing $name"}}
    foreach($entry in @(@('RemoveStratagemEnemy','Prefix'),@('OtherworldUnit','OnDisable'))){
        $method=(Get-ModType $entry[0]).Methods | Where-Object Name -EQ $entry[1]
        if(-not (Calls $method 'Removed')){throw "Native/unit removal bypasses common lifecycle: $($entry[0])"}
    }
    'PASS: compiled transfer, pending physics move, five coroutines and removal lifecycle wiring'
} finally {$assembly.Dispose()}
