#version 450 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec4 u_bc_color;
uniform vec3 u_camera_position;
uniform float u_absortion_coef;

uniform float u_step_length;
uniform bool u_volume_type;  // 0 = homogeneous, 1 = heterogeneous
uniform float u_noise_scale;
uniform float u_noise_detail;

//New for task 4
uniform bool u_shader_type;
uniform vec4 u_emission_color;       // Emission color for the volume

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
    float accumulatedTransmittance = 1.0;
    vec4 accumulatedEmission = vec4(0.0);

    while (t < tFar) {
        vec3 samplePos = rayOrigin + t * rayDir;
        
        // Determine the absorption coefficient
        float absorption = u_absortion_coef;
        if (u_volume_type) {
            // If the volume is hereogeniuos we add the noise
            absorption *= noise(samplePos * u_noise_scale) * u_noise_detail;
        }

        // Compute transmittance at this step
        float transmittance = exp(-absorption * u_step_length);

        if (u_shader_type) {
            // Emission-Absorption Model
            accumulatedEmission += u_emission_color * (1.0 - transmittance) * accumulatedTransmittance;
            accumulatedTransmittance *= transmittance;
        } else {
            // Accumulate optical thickness for Absorption-only Model
            opticalThickness += absorption * u_step_length;
        }

        // Advance the ray
        t += u_step_length;
    }

    if (u_shader_type) {
        // Emission-Absorption Model: combine emission and background color
        FragColor = accumulatedEmission + u_bc_color * accumulatedTransmittance;
    } else {
        // Absorption-only Model: calculate transmittance based on optical thickness
        float finalTransmittance = exp(-opticalThickness);
        FragColor = u_bc_color * finalTransmittance;
    }

}