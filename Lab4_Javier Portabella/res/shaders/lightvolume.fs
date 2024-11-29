#version 450 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec4 u_bc_color;
uniform vec3 u_camera_position;
uniform float u_absortion_coef;
uniform float u_step_length;
uniform float u_noise_scale;
uniform float u_noise_detail;

//New for Lab 4
uniform sampler3D u_texture;
uniform int u_density_type;

uniform vec3 u_light_position;
uniform float u_light_intensity;
uniform vec3 u_light_color;
uniform vec3 u_light_direction;
uniform float u_scattering; // Scattering coefficient

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
    float transmittance = 1.0;
    vec3 totalLight = vec3(0.0); // Here is where we accumulate the light
    vec4 accumulatedEmission = vec4(0.0);
    vec3 emissiveContribution = vec3(0.0);
    
    while (t < tFar) {
        vec3 samplePos = rayOrigin + t * rayDir;
        float absorption = u_absortion_coef;

        // We check which density we are using -> 0=rabbit 1=noise cube 2=regular cube
        if (int(u_density_type) == 0) {
            vec3 samplePosVDB = (samplePos + 1.0) / 2.0; // normalize between 0 and 1
            float density = texture(u_texture, samplePosVDB).r;
            
            absorption *= density;
            float scattering = u_scattering * density;

            // Computing volumentric light
            vec3 lightDir = normalize(u_light_position - samplePos); // Light direction
            vec2 lightTValues = intersectAABB(samplePos, lightDir, boxMin, boxMax);

            float lightTNear = max(lightTValues.x, 0.0);
            float lightTFar = lightTValues.y;      //limits for the new marching
            float lightT = lightTNear;

            float lightTransmittance = 1.0;
            vec3 inScatteredLight = vec3(0.0);

            while (lightT < lightTFar) {   //marching again to compute scattering
                vec3 lightSamplePos = (samplePos + lightT * lightDir + 1.0) / 2.0; // normalize between 0 and 1
                float lightDensity = texture(u_texture, lightSamplePos).r;

                float lightOpticalThickness = lightDensity * u_step_length;
                float lightStepTransmittance = exp(-lightOpticalThickness);
                lightTransmittance *= lightStepTransmittance;

                if (lightTransmittance < 0.01) break;

                lightT += u_step_length;
            }

            inScatteredLight = u_light_color.rgb * u_light_intensity * lightTransmittance;  //after the loop we compute the scatering value Ls(t')

            // Here we multiply the scattering term and the emissive term
            vec3 scatteringContribution = scattering * inScatteredLight;        //𝜇s(t') x Ls(t')
            //emissiveContribution = (absorption + scattering) * emissive light   --> we do not have the emissive light bc in the last lab we couldn't do it. 
            //float mu_a = u_absortion_coef * density;
            //float mu_s = u_scattering * density;
            //float mu_t = mu_a + mu_s;        --> extintion coefficient
            accumulatedEmission.rgb += scatteringContribution * transmittance;

        } else if (int(u_density_type) == 1) {
            absorption *= noise(samplePos * u_noise_scale) * u_noise_detail;
        }
        
        opticalThickness += absorption * u_step_length;

        t += u_step_length; // Advance in the marching
    }


    
    // Computation of the final transmittance
    transmittance = exp(-opticalThickness);

    // We combine the background color with the accumulated light
    vec3 finalColor = u_bc_color.rgb * transmittance;
    FragColor = accumulatedEmission + vec4(finalColor, u_bc_color.a);

}