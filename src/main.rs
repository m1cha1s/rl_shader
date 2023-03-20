use raylib::prelude::*;

const MAX_SPHERES: usize = 20;

#[repr(C)]
#[derive(Clone, Copy)]
struct Sphere {
    center: [f32; 3],
    radius: f32,
    center_loc: i32,
    radius_loc: i32,
}

impl Sphere {
    fn new(center: [f32; 3], radius: f32) -> Self {
        Self {
            center,
            radius,
            center_loc: -1,
            radius_loc: -1,
        }
    }

    fn set_loc(&mut self, center_loc: i32, radius_loc: i32) {
        self.center_loc = center_loc;
        self.radius_loc = radius_loc;
    }

    fn set_sphere(&self, shdr: &mut Shader) {
        shdr.set_shader_value(self.center_loc, self.center);
        shdr.set_shader_value(self.radius_loc, self.radius);
    }
}

struct Spheres {
    spheres: Vec<Sphere>,
    sphere_cnt_loc: i32,
}

#[derive(Debug)]
enum SphereErr {
    ToManySpheres,
}
impl Spheres {
    fn new(shdr: &mut Shader) -> Self {
        Self {
            spheres: Vec::new(),
            sphere_cnt_loc: shdr.get_shader_location("sphereCnt"),
        }
    }

    fn add_sphere(&mut self, shdr: &mut Shader, mut sphere: Sphere) {
        sphere.set_loc(
            shdr.get_shader_location(format!("spheres[{}].center", self.spheres.len()).as_str()),
            shdr.get_shader_location(format!("spheres[{}].radius", self.spheres.len()).as_str()),
        );
        self.spheres.push(sphere);
        shdr.set_shader_value(self.sphere_cnt_loc, self.spheres.len() as i32);
    }

    fn set_spheres(&self, shdr: &mut Shader) -> Result<(), SphereErr> {
        if self.spheres.len() > MAX_SPHERES {
            return Err(SphereErr::ToManySpheres);
        }

        for sphere in self.spheres.iter() {
            sphere.set_sphere(shdr);
        }

        Ok(())
    }
}

fn main() {
    let (mut rl, thread) = raylib::init().size(800, 800).title("Shader").build();

    let mut shader = match rl.load_shader(&thread, None, Some("shaders/rt.fs")) {
        Ok(shad) => shad,
        Err(err) => {
            panic!("Shader err: {}", err)
        }
    };

    let resolution_loc = shader.get_shader_location("resolution");

    let resolution = [800.0, 800.0];
    shader.set_shader_value(resolution_loc, resolution);

    let mut spheres = Spheres::new(&mut shader);
    spheres.set_spheres(&mut shader).unwrap();

    spheres.add_sphere(&mut shader, Sphere::new([0.0, 0.0, -1.0], 0.5));
    spheres.add_sphere(&mut shader, Sphere::new([0.0, -100.5, -1.0], 100.0));

    rl.set_target_fps(60);

    while !rl.window_should_close() {
        _ = spheres.set_spheres(&mut shader);

        let mut d = rl.begin_drawing(&thread);
        d.clear_background(Color::RAYWHITE);

        let mut s = d.begin_shader_mode(&shader);

        s.draw_rectangle(0, 0, 800, 800, Color::WHITE);
        drop(s);

        d.draw_text("shader test", 0, 0, 12, Color::BLACK);
    }
}
