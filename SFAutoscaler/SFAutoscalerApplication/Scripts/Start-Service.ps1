$cloud = $false
$singleNode = $true
$constrainedNodeTypes = $false

$lowkey = "-9223372036854775808"
$highkey = "9223372036854775807" 

$appName = "fabric:/SFAutoscalerApp"
$appType = "SFAutoscalerApplicationType"
$appInitialVersion = "1.0.0"

if($singleNode)
{
    #$webServiceInstanceCount = -1
    #$bandCreationInstanceCount = -1
    #$countyServicePartitionCount = 1
    #$bandActorServicePartitionCount = 1
    #$doctorServicePartitionCount = 1
}
else
{
    #$webServiceInstanceCount = @{$true=-1;$false=1}[$cloud -eq $true] 
    #$bandCreationInstanceCount = @{$true=-1;$false=1}[$cloud -eq $true] 
    #$countyServicePartitionCount = @{$true=10;$false=5}[$cloud -eq $true]  
    #$bandActorServicePartitionCount = @{$true=15;$false=5}[$cloud -eq $true]  
    #$doctorServicePartitionCount = @{$true=100;$false=5}[$cloud -eq $true]  

#	$webServiceConstraint = ""
#   $countyServiceConstraint = ""
#   $nationalServiceConstraint = ""
#   $bandServiceConstraint = ""
#   $doctorServiceConstraint = ""
#   $bandCreationServiceConstraint = ""   
}

#$autoscalerType = "HealthMetrics.WebServiceType"
#$webServiceName = "HealthMetrics.WebService"

#$nationalServiceType = "HealthMetrics.NationalServiceType"
#$nationalServiceName = "HealthMetrics.NationalService"
#$nationalServiceReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]  

#$countyServiceType = "HealthMetrics.CountyServiceType"
#$countyServiceName = "HealthMetrics.CountyService"
#$countyServiceReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]  

#$bandCreationServiceType = "HealthMetrics.BandCreationServiceType"
#$bandCreationServiceName = "HealthMetrics.BandCreationService"

#$doctorServiceType = "HealthMetrics.DoctorServiceType"
#$doctorServiceName = "HealthMetrics.DoctorService"
#$doctorServiceReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]

#$bandActorServiceType = "BandActorServiceType"
#$bandActorServiceName= "HealthMetrics.BandActorService"
#$bandActorReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]

#New-ServiceFabricService -ServiceTypeName $webServiceType -Stateless -ApplicationName $appName -ServiceName "$appName/$webServiceName" -PartitionSchemeSingleton -InstanceCount $webServiceInstanceCount -PlacementConstraint $webServiceConstraint -ServicePackageActivationMode ExclusiveProcess
