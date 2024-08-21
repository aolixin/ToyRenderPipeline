sampler2D _gdepth;
sampler2D _GT0;
sampler2D _GT1;
sampler2D _GT2;
sampler2D _GT3;

samplerCUBE _diffuseIBL;
samplerCUBE _specularIBL;
sampler2D _brdfLut;

sampler2D _shadowtex0;
sampler2D _shadowtex1;
sampler2D _shadowtex2;
sampler2D _shadowtex3;
sampler2D _shadoMask;

float4x4 _shadowVpMatrix0;
float4x4 _shadowVpMatrix1;
float4x4 _shadowVpMatrix2;
float4x4 _shadowVpMatrix3;

float _split0;
float _split1;
float _split2;
float _split3;

float _shadowMapResolution;