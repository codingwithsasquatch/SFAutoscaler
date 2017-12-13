$i = 0
$target = 10

while($i -lt $target)
{
    New-ServiceFabricService -ServiceTypeName LoadGeneratorServiceType -Stateless -ApplicationName "fabric:/SFAutoscalerApplication" -ServiceName "fabric:/SFAutoscalerApplication/LoadGen$i" -PartitionSchemeSingleton -InstanceCount 1 -ServicePackageActivationMode ExclusiveProcess
    $i++;
}