//#ifndef TESSELLATION_CGINC_INCLUDED
//#define TESSELLATION_CGINC_INCLUDED
#if defined(SHADER_API_D3D11) ||defined(SHADER_API_D3D12) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)
    #define UNITY_CAN_COMPILE_TESSELLATION 1
    #   define UNITY_domain                 domain
    #   define UNITY_partitioning           partitioning
    #   define UNITY_outputtopology         outputtopology
    #   define UNITY_patchconstantfunc      patchconstantfunc
    #   define UNITY_outputcontrolpoints    outputcontrolpoints
#endif
 
struct Varyings2
{       
    float3 worldPos : TEXCOORD1;
    float3 normal : NORMAL;
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 viewDir : TEXCOORD3;
    float fogFactor : TEXCOORD4;
    UNITY_VERTEX_OUTPUT_STEREO
     UNITY_VERTEX_INPUT_INSTANCE_ID
 
};
 
float _Tess;
float _MaxTessDistance;
 
struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};
 
struct Attributes2
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;    
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
 
struct ControlPoint
{
    float4 vertex : INTERNALTESSPOS;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;  
    UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
};
 
[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("fractional_odd")]
[UNITY_patchconstantfunc("patchConstantFunction")]
ControlPoint hull(InputPatch<ControlPoint, 3> patch, uint id : SV_OutputControlPointID)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(patch[id]);
    return patch[id];
}
 
TessellationFactors UnityCalcTriEdgeTessFactors (float3 triVertexFactors)
{
    TessellationFactors tess;
    tess.edge[0] = 0.5 * (triVertexFactors.y + triVertexFactors.z);
    tess.edge[1] = 0.5 * (triVertexFactors.x + triVertexFactors.z);
    tess.edge[2] = 0.5 * (triVertexFactors.x + triVertexFactors.y);
    tess.inside = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
    return tess;
}
 
float CalcDistanceTessFactor(float4 vertex, float minDist, float maxDist, float tess)
{
                float3 worldPosition = mul(unity_ObjectToWorld, vertex).xyz;
                float dist = distance(worldPosition, _WorldSpaceCameraPos);
                float f = clamp(1.0 - (dist - minDist) / (maxDist), 0, 1.0);
                
                return (f * tess) + 1;
}
 
TessellationFactors DistanceBasedTess(float4 v0, float4 v1, float4 v2, float minDist, float maxDist, float tess)
{
                float3 f;
                f.x = CalcDistanceTessFactor(v0, minDist, maxDist, tess);
                f.y = CalcDistanceTessFactor(v1, minDist, maxDist, tess);
                f.z = CalcDistanceTessFactor(v2, minDist, maxDist, tess);
 
                return UnityCalcTriEdgeTessFactors(f);
}
 
uniform float3 _Position;
uniform sampler2D _GlobalEffectRT;
uniform float _OrthographicCamSize;
 
sampler2D  _Noise;
float _NoiseScale, _SnowHeight, _NoiseWeight, _SnowDepth;
 
TessellationFactors patchConstantFunction(InputPatch<ControlPoint, 3> patch)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(patch[0]);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(patch[1]);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(patch[2]);
    float minDist = 2.0;
    float maxDist = _MaxTessDistance;
    TessellationFactors f;
    return DistanceBasedTess(patch[0].vertex, patch[1].vertex, patch[2].vertex, minDist, maxDist, _Tess);
   
}
 
float4 GetShadowPositionHClip(Attributes2 input)
{
    float3 positionWS = TransformObjectToWorld(input.vertex.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normal);
 
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, 0));
 
#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif
    return positionCS;
}
float _PathCutoff, _PathSmooth;
float GetDisplacement(float3 worldPosition)
{
    float2 uv = worldPosition.xz - _Position.xz;
    uv = uv / (_OrthographicCamSize * 2);
    uv += 0.5;
 
    float4 RTEffect = tex2Dlod(_GlobalEffectRT, float4(uv, 0, 0));
    RTEffect *= smoothstep(0.99, 0.9, uv.x)     * smoothstep(0.99, 0.9, 1.0 - uv.x);
    RTEffect *= smoothstep(0.99, 0.9, uv.y)     * smoothstep(0.99, 0.9, 1.0 - uv.y);
 
    float noise = tex2Dlod(_Noise, float4(worldPosition.xz * _NoiseScale, 0, 0)).r;
    float baseSnowHeight = saturate(_SnowHeight + noise * _NoiseWeight);
    RTEffect.g =  smoothstep(_PathCutoff,_PathCutoff + _PathSmooth,RTEffect.g);
    float pathSnow = saturate(1.0 - RTEffect.g * _SnowDepth);
   
    return  baseSnowHeight * pathSnow;
}
 
Varyings2 vert(Attributes2 input)
{
    Varyings2 output = (Varyings2)0;
     UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
 
    float3 worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
    float3 worldNormal = SafeNormalize(mul((float3x3)unity_ObjectToWorld, input.normal));
 
   // add vertex dispalcement from path
    float d = GetDisplacement(worldPosition);
    input.vertex.xyz += SafeNormalize(input.normal) * d;
    worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz; // update after displacement
 
    //recalculated normal with displacement.
    float eps = 0.01;
    float dx = GetDisplacement(worldPosition + float3(eps, 0, 0));
    float dz = GetDisplacement(worldPosition + float3(0, 0, eps));
 
    // tangent calculation
    float3 tangentX  = normalize(float3(eps, 0, 0) + worldNormal * (dx - d));
    float3 tangentZ  = normalize(float3(0, 0, eps) + worldNormal * (dz - d));
    float3 newWorldNormal = normalize(cross(tangentZ, tangentX));
 
    // back to object space
    output.normal = normalize(mul((float3x3)unity_WorldToObject, newWorldNormal));
 
    output.viewDir = SafeNormalize(GetCameraPositionWS() - worldPosition);
 
    #ifdef SHADERPASS_SHADOWCASTER
        output.vertex = GetShadowPositionHClip(input);
    #else
        output.vertex = TransformObjectToHClip(input.vertex.xyz);
    #endif
 
    output.worldPos = worldPosition;
    output.uv = input.uv;
    output.fogFactor = ComputeFogFactor(output.vertex.z);
 
    
 
    return output;
}
 
[UNITY_domain("tri")]
Varyings2 domain(TessellationFactors factors, OutputPatch<ControlPoint, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(patch[0]);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(patch[1]);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(patch[2]);
    Attributes2 v = (Attributes2)0;
    
    #define Interpolate(fieldName) v.fieldName = \
                patch[0].fieldName * barycentricCoordinates.x + \
                patch[1].fieldName * barycentricCoordinates.y + \
                patch[2].fieldName * barycentricCoordinates.z;
 
    Interpolate(vertex)
    Interpolate(uv)
    Interpolate(normal)
    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(patch[0], v)
 
    return vert(v);
}