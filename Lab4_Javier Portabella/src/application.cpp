#include "application.h"

bool render_wireframe = false;
Camera* Application::camera = nullptr;

void Application::init(GLFWwindow* window)
{
    this->instance = this;
    glfwGetFramebufferSize(window, &this->window_width, &this->window_height);

    // OpenGL flags
    glEnable(GL_CULL_FACE); // render both sides of every triangle
    glEnable(GL_DEPTH_TEST); // check the occlusions using the Z buffer

    // Create camera
    this->camera = new Camera();
    this->camera->lookAt(glm::vec3(1.f, 1.5f, 4.f), glm::vec3(0.f, 0.0f, 0.f), glm::vec3(0.f, 1.f, 0.f));
    this->camera->setPerspective(60.f, this->window_width / (float)this->window_height, 0.1f, 500.f); // set the projection, we want to be perspective

    this->flag_grid = true;
    this->flag_wireframe = false;

    this->ambient_light = glm::vec4(0.75f, 0.75f, 0.75f, 1.f);

    this->bc_color = glm::vec4(1.0f, 0.5f, 0.5f, 1.f);
    this->absortion_coef = 1.0f;

    this->step_length = 0.009f; // We need to set a small number to avoid seeing lines in the cube. 
    
    //Lab 3
    //this->volume_type = true;
    this->noise_scale = 2.0f;
    this->noise_detail = 2.0f;
    this->shader_type = false;
    //this->emission_color = glm::vec4(1.0f, 0.0f, 0.0f, 1.f);

    //Lab 4
    this->density_type = 0; //we set by default the rabbit texture

    /* ADD NODES TO THE SCENE */
    VolumeSceneNode* example = new VolumeSceneNode();

    this->light_position = glm::vec3(1.0f, 1.0f, 1.0f);    //we set the values to enter into the new Light
    this->light_intensity = 1.f;
    this->light_color = glm::vec4(1.f, 1.f, 1.f, 1.f);
    Light* pointLight = new Light(light_position, LIGHT_POINT, light_intensity, light_color);

    this->light_list.push_back(pointLight);                //we add the new light into the list of lights
    this->node_list.push_back(example);
}

void Application::update(float dt)
{
    // mouse update
    glm::vec2 delta = this->lastMousePosition - this->mousePosition;
    if (this->dragging) {
        this->camera->orbit(-delta.x * dt, delta.y * dt);
    }
    this->lastMousePosition = this->mousePosition;
}

void Application::render()
{
    glClearColor(bc_color.x, bc_color.y, bc_color.z, 1.0f);

    // Clear the window and the depth buffer
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    // set flags
    glEnable(GL_DEPTH_TEST);
    glEnable(GL_CULL_FACE);

    for (unsigned int i = 0; i < this->node_list.size(); i++)
    {
        this->node_list[i]->render(this->camera);

        if (this->flag_wireframe) this->node_list[i]->renderWireframe(this->camera);
    }
    
    for (unsigned int i = 0; i < this->light_list.size(); i++)
    {
        this->light_list[i]->render(this->camera);

    }

    // Draw the floor grid
    if (this->flag_grid) drawGrid();
}

void Application::renderGUI()
{
    if (ImGui::TreeNodeEx("Scene", ImGuiTreeNodeFlags_DefaultOpen))
    {
        //ImGui::ColorEdit3("Ambient light", (float*)&this->ambient_light);      //This is not useful now
        ImGui::ColorEdit3("Background color", (float*)&this->bc_color);


        if (ImGui::TreeNode("Camera")) {
            this->camera->renderInMenu();
            ImGui::TreePop();
        }

        unsigned int count = 0;
        std::stringstream ss;
        for (auto& node : this->node_list) {
            ss << count;
            if (ImGui::TreeNode(node->name.c_str())) {
                
                //Lab 3         
                //ImGui::SliderFloat("Noise Scale", &this->noise_scale, 0.0, 2.0);
                //ImGui::SliderFloat("Noise Detail", &this->noise_detail, 0.0, 6.0);    //We put these controls here to be within the node dropdown.
                //ImGui::Checkbox("Emision-Absortion shader", &this->shader_type);      //We choose the ones that are characteristics of the node.
                //ImGui::ColorEdit3("Emission Color", (float*)&this->emission_color);

                //Lab 4
                static int index = 0;
                ImGui::Combo("Density Options", &index,"VDB File\0Noise\0Constant Density\0");
                if (index == 0) { density_type = 0; }
                else if (index == 1) { density_type = 1; }          // Combo with the 3 density options
                else if (index == 2) { density_type = 2; };

                node->renderInMenu();
                ImGui::TreePop();
            }
        }

        count = 0;
        if (ImGui::TreeNode("Lights")) {                            //We add a new dropdown for the light information
            for (auto& light : this->light_list) {
                ss.str("");
                ss << count;
                if (ImGui::TreeNode(("Light" + std::to_string(count)).c_str())) {
                    light->renderInMenu();
                    ImGui::TreePop();
                }
                count++;
            }
            ImGui::TreePop();
        }

        ImGui::TreePop();
    }
}

void Application::shutdown() { }

// keycodes: https://www.glfw.org/docs/3.3/group__keys.html
void Application::onKeyDown(int key, int scancode)
{
    switch (key) {
    case GLFW_KEY_ESCAPE: // quit
        close = true;
        break;
    case GLFW_KEY_R:
        Shader::ReloadAll();
        break;
    }
}

// keycodes: https://www.glfw.org/docs/3.3/group__keys.html
void Application::onKeyUp(int key, int scancode)
{
    switch (key) {
    case GLFW_KEY_T:
        std::cout << "T released" << std::endl;
        break;
    }
}

void Application::onRightMouseDown()
{
    this->dragging = true;
    this->lastMousePosition = this->mousePosition;
}

void Application::onRightMouseUp()
{
    this->dragging = false;
    this->lastMousePosition = this->mousePosition;
}

void Application::onLeftMouseDown()
{
    this->dragging = true;
    this->lastMousePosition = this->mousePosition;
}

void Application::onLeftMouseUp()
{
    this->dragging = false;
    this->lastMousePosition = this->mousePosition;
}

void Application::onMiddleMouseDown() { }

void Application::onMiddleMouseUp() { }

void Application::onMousePosition(double xpos, double ypos) { }

void Application::onScroll(double xOffset, double yOffset)
{
    int min = this->camera->min_fov;
    int max = this->camera->max_fov;

    if (yOffset < 0) {
        this->camera->fov += 4.f;
        if (this->camera->fov > max) {
            this->camera->fov = max;
        }
    }
    else {
        this->camera->fov -= 4.f;
        if (this->camera->fov < min) {
            this->camera->fov = min;
        }
    }
    this->camera->updateProjectionMatrix();
}