#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 finalColor;

uniform vec2 resolution;
uniform int antiAliasing;

// FIXME: This may be slow
float rand(vec2 co){
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

vec3 random_vec3(vec2 uv) {
    return vec3(rand(uv), rand(uv), rand(uv));
}

vec3 random_in_unit_sphere(vec2 uv) {
    while (true) {
        vec3 p = random_vec3(uv);
        if (dot(p, p) >= 1) { continue; }
        return p;
    }
}

struct Sphere {
    vec3 center;
    float radius;
};

uniform Sphere spheres[20];
uniform int sphereCnt;

struct Ray {
    vec3 origin;
    vec3 direction;
};

vec3 ray_at(Ray r, float t) {
    return r.origin + (t*r.direction);
}

struct HitRecord {
    bool hit;
    vec3 p;
    vec3 normal;
    float t;
};

vec3 set_face_normal(Ray r, vec3 outward_normal) {
    bool front_face = dot(r.direction, outward_normal) < 0;
    if (front_face) {
        return outward_normal;
    } else {
        return outward_normal * -1;
    }
}

HitRecord hit_sphere(Sphere sphere, Ray r, float t_min, float t_max) {
    vec3 oc = r.origin - sphere.center;
    float a = dot(r.direction, r.direction);
    float half_b = dot(oc, r.direction);
    float c = dot(oc, oc) - sphere.radius*sphere.radius;
    float discriminant = half_b*half_b - a*c;

    HitRecord rec = HitRecord(false, vec3(0), vec3(0), 0);

    if (discriminant < 0) {
        return rec;
    }
    
    float sqrtd = sqrt(discriminant);

    float root = (-half_b - sqrtd) / a;
    if (root < t_min || t_max < root) {
        root = (-half_b + sqrtd) / a;
        if (root < t_min || t_max < root){
            return rec;
        }
    }

    rec.t = root;
    rec.p = ray_at(r, root);
    vec3 outward_normal = (rec.p - sphere.center) / sphere.radius;

    rec.normal = set_face_normal(r, outward_normal);

    rec.hit = true;

    return rec;
}

HitRecord hit_spheres(Ray r, float t_min, float t_max) {
    HitRecord rec;
    bool hit_anything = false;
    float closest_so_far = t_max;

    for (int i = 0; i < sphereCnt; i ++) {
        HitRecord temp_rec = hit_sphere(spheres[i], r, t_min, closest_so_far);

        if (temp_rec.hit) {
            hit_anything = true;
            closest_so_far = temp_rec.t;
            rec = temp_rec;
        }
    }

    rec.hit = hit_anything;

    return rec;
}

vec4 ray_color(Ray r) {
    HitRecord rec = hit_spheres(r, 0, 99999999);
    if(rec.hit) {
        vec3 target = rec.p + rec.normal + random_in_unit_sphere(gl_FragCoord.xy);
        return vec4((0.5*ray_color(Ray(rec.p, target-rec.p))).xyz, 1);
    }
    vec3 unit_direction = normalize(r.direction);
    float t = 0.5 * (unit_direction.y + 1.0);
    return vec4(((1.0-t)*vec3(1.0,1.0,1.0) + t*vec3(0.5,0.7,1.0)).xyz, 1.0);
}

void main() {
    float aspect_ratio = resolution.x/resolution.y;

    float viewport_height = 2.0;
    float viewport_width = aspect_ratio * viewport_height;
    float focal_lenght = 1.0;

    vec3 origin = vec3(0.0);
    vec3 horizontal = vec3(viewport_width,0.0,0.0);
    vec3 vertical = vec3(0.0, viewport_height, 0.0);
    vec3 lower_left_corner = origin - horizontal/2.0 - vertical/2.0 - vec3(0.0,0.0,focal_lenght);

    vec4 color = vec4(0);
    vec2 uv = vec2(0);

    for (int i=0;i<antiAliasing;i++) {
        uv = (gl_FragCoord.xy+vec2(rand(uv), rand(uv)))/resolution;
        Ray r = Ray(origin, lower_left_corner + uv.x*horizontal + uv.y*vertical - origin);

        color += ray_color(r);
    }

    finalColor = color/float(antiAliasing);
}