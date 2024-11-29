#version 450 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec4 u_bc_color;
uniform vec3 u_camera_position;
uniform float u_absortion_coef;

uniform float u_step_length;
uniform int u_density_type;
uniform float u_noise_scale;
uniform float u_noise_detail;

//New for Lab 4
uniform sampler3D u_texture;

out vec4 FragColor;

vec2 intersectAABB(vec3 rayOrigin, vec3 rayDir, vec3 boxMin, vec3 boxMax) {
    vec3 tMin = (boxMin - rayOrigin) / rayDir;
    vec3 tMax = (boxMax - rayOrigin) / rayDir;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);
    return vec2(tNear, tFar);
}

//  -------------------------- FUNCTION GIVEN TO GENERATE THE NOISE --------------------------
float mod289(float x){return x - floor(x * (1.0 / 289.0)) * 289.0;}
vec4 mod289(vec4 x){return x - floor(x * (1.0 / 289.0)) * 289.0;}
vec4 perm(vec4 x){return mod289(((x * 34.0) + 1.0) * x);}

float noise(vec3 p){
    vec3 a = floor(p);
    vec3 d = p - a;
    d = d * d * (3.0 - 2.0 * d);

    vec4 b = a.xxyy + vec4(0.0, 1.0, 0.0, 1.0);
    vec4 k1 = perm(b.xyxy);
    vec4 k2 = perm(k1.xyxy + b.zzww);

    vec4 c = k2 + a.zzzz;
    vec4 k3 = perm(c);
    vec4 k4 = perm(c + 1.0);

    vec4 o1 = fract(k3 * (1.0 / 41.0));
    vec4 o2 = fract(k4 * (1.0 / 41.0));

    vec4 o3 = o2 * d.z + o1 * (1.0 - d.z);
    vec2 o4 = o3.yw * d.x + o3.xz * (1.0 - d.x);

    return o4.y * d.y + o4.x * (1.0 - d.y);
} // ---------------------------------------------------------------------------------------------


void main()
{
	vec3 rayOrigin = u_camera_position;        //First we initalize the ray
	vec3 rayDir = normalize(v_world_position - u_camera_position);
	
    vec3 boxMin = vec3(-1.0, -1.0, -1.0);      //THen we compute the intersaction with the volume
    vec3 boxMax = vec3(1.0, 1.0, 1.0);
    vec2 tValues = intersectAABB(rayOrigin, rayDir, boxMin, boxMax);

    float tNear = tValues.x;
    float tFar = tValues.y;
	    if (tNear > tFar) {
        discard;
    }

    float opticalThickness = 0.0;
    float t = tNear;
    //float stepTransmittance = 1.0;

    while (t < tFar) {
        vec3 samplePos = rayOrigin + t * rayDir;
        float absorption = u_absortion_coef;

        if (int(u_density_type) == 0) {
            vec3 samplePos = (samplePos + 1) /2.0;
            absorption *= texture(u_texture, samplePos).r;
        }
        else if(int(u_density_type) == 1){
            absorption *= noise(samplePos * u_noise_scale) * u_noise_detail;
        }
        // Compute transmittance at this step
        opticalThickness += absorption * u_step_length;

        // Avanzar el paso 
        t += u_step_length;
    }

    // Calcular transmitancia final
    float transmittance = exp(-opticalThickness);
    FragColor = u_bc_color * transmittance;
}